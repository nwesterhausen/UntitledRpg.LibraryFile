using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace UntitledRpg.LibraryFile;

/// <summary>
///     Fluent builder for constructing and saving .urpglib packages.
/// </summary>
public sealed class UrpglibPackageBuilder
{
	private readonly List<Func<TarWriter, CancellationToken, Task>> entryWriters = [];
	private PayloadCompressionType compression = PayloadCompressionType.Gzip;
	private PackageManifest? manifest;

	/// <summary>
	///     Sets the package manifest metadata.
	/// </summary>
	public UrpglibPackageBuilder WithManifest(PackageManifest packageManifest)
	{
		this.manifest = packageManifest ?? throw new ArgumentNullException(nameof(packageManifest));
		return this;
	}

	/// <summary>
	///     Sets the payload compression algorithm. Defaults to Gzip.
	/// </summary>
	public UrpglibPackageBuilder WithCompression(PayloadCompressionType payloadCompressionType)
	{
		this.compression = payloadCompressionType;
		return this;
	}

	/// <summary>
	///     Adds a single file from disk into the package payload.
	/// </summary>
	public UrpglibPackageBuilder AddFile(string sourceFilePath, string entryName)
	{
		ArgumentException.ThrowIfNullOrEmpty(sourceFilePath);
		ArgumentException.ThrowIfNullOrEmpty(entryName);

		this.entryWriters.Add(async (writer, ct) =>
		{
			var stream = File.OpenRead(sourceFilePath);
			await using (stream.ConfigureAwait(false))
			{
				var entry = new PaxTarEntry(TarEntryType.RegularFile, entryName.Replace('\\', '/'))
				{
					DataStream = stream
				};
				await writer.WriteEntryAsync(entry, ct).ConfigureAwait(false);
			}
		});

		return this;
	}

	/// <summary>
	///     Adds raw bytes as an entry in the package payload.
	/// </summary>
	public UrpglibPackageBuilder AddBytes(byte[] content, string entryName)
	{
		ArgumentNullException.ThrowIfNull(content);
		ArgumentException.ThrowIfNullOrEmpty(entryName);

		this.entryWriters.Add(async (writer, ct) =>
		{
			using var stream = new MemoryStream(content);
			var entry = new PaxTarEntry(TarEntryType.RegularFile, entryName.Replace('\\', '/')) { DataStream = stream };
			await writer.WriteEntryAsync(entry, ct).ConfigureAwait(false);
		});

		return this;
	}

	/// <summary>
	///     Recursively adds all files from a directory into the payload.
	/// </summary>
	public UrpglibPackageBuilder AddDirectory(string sourceDirectory, string entryPrefix = "")
	{
		ArgumentException.ThrowIfNullOrEmpty(sourceDirectory);

		foreach (var filePath in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
		{
			var relativePath = Path.GetRelativePath(sourceDirectory, filePath).Replace('\\', '/');
			var entryName = string.IsNullOrEmpty(entryPrefix)
				? relativePath
				: $"{entryPrefix.TrimEnd('/')}/{relativePath}";
			this.AddFile(filePath, entryName);
		}

		return this;
	}

	/// <summary>
	///     Builds the package and writes it to a file.
	/// </summary>
	public async Task BuildAsync(string destinationFilePath, CancellationToken ct = default)
	{
		var dir = Path.GetDirectoryName(destinationFilePath);
		if (!string.IsNullOrEmpty(dir))
		{
			Directory.CreateDirectory(dir);
		}

		var fs = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
		await using (fs.ConfigureAwait(false))
		{
			await this.BuildAsync(fs, ct).ConfigureAwait(false);
		}
	}

	/// <summary>
	///     Builds the package and writes it into the target stream.
	/// </summary>
	public async Task BuildAsync(Stream outputStream, CancellationToken ct = default)
	{
		if (this.manifest is null)
		{
			throw new InvalidOperationException("Cannot build package without a PackageManifest.");
		}

		ArgumentNullException.ThrowIfNull(outputStream);

		// 1. Serialize Manifest
		var manifestBytes =
			JsonSerializer.SerializeToUtf8Bytes(this.manifest, UrpglibConstants.DefaultJsonSerializerOptions);

		// 2. Write 18-byte Binary Header
		var header = new byte[UrpglibConstants.MinFileSize];
		UrpglibConstants.MagicBytes.CopyTo(header, 0);
		header[8] = UrpglibConstants.CurrentHeaderSchemaVersion;
		BitConverter.TryWriteBytes(header.AsSpan(9, 2), UrpglibConstants.CurrentManifestSchemaVersion);
		header[11] = (byte)this.compression;
		BitConverter.TryWriteBytes(header.AsSpan(12, 2), UrpglibConstants.CurrentPayloadSchemaVersion);
		BitConverter.TryWriteBytes(header.AsSpan(14, 4), (uint)manifestBytes.Length);

		await outputStream.WriteAsync(header, ct).ConfigureAwait(false);
		await outputStream.WriteAsync(manifestBytes, ct).ConfigureAwait(false);

		// 3. Write Compressed Tar Stream
		var payloadCompressionStream = this.compression switch
		{
			PayloadCompressionType.Gzip => new GZipStream(outputStream, CompressionLevel.Optimal, true),
			PayloadCompressionType.None => outputStream,
			_ => throw new NotSupportedException($"Compression {this.compression} not supported.")
		};
		await using (payloadCompressionStream.ConfigureAwait(false))
		{
			var tarWriter = new TarWriter(payloadCompressionStream, TarEntryFormat.Pax, true);
			await using (tarWriter.ConfigureAwait(false))
			{
				foreach (var writerFunc in this.entryWriters)
				{
					await writerFunc(tarWriter, ct).ConfigureAwait(false);
				}
			}
		}

		await outputStream.FlushAsync(ct).ConfigureAwait(false);
	}
}
