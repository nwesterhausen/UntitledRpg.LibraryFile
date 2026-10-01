using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace UntitledRpg.LibraryFile;

/// <summary>
///     Represents the complete, deserialized .urpglib package,
///     providing access to its manifest and a stream for its payload data.
/// </summary>
public sealed class UrpglibPackage : IDisposable, IAsyncDisposable
{
	private readonly bool ownsStream;
	private readonly long payloadStartPosition;
	private readonly Stream payloadStream;
	private bool disposed;

	internal UrpglibPackage(
		UrpglibHeader header,
		PackageManifest? manifest,
		Stream payloadStream,
		long payloadStartPosition = 0,
		bool ownsStream = true)
	{
		this.Header = header;
		this.Manifest = manifest;
		this.payloadStream = payloadStream;
		this.payloadStartPosition = payloadStartPosition;
		this.ownsStream = ownsStream;
	}

	/// <summary>
	///     The structured header data read from the file.
	/// </summary>
	public UrpglibHeader Header { get; }

	/// <summary>
	///     The deserialized manifest containing package metadata.
	/// </summary>
	public PackageManifest? Manifest { get; }

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (this.disposed)
		{
			return;
		}

		if (this.ownsStream)
		{
			await this.payloadStream.DisposeAsync().ConfigureAwait(false);
		}

		this.disposed = true;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (this.disposed)
		{
			return;
		}

		if (this.ownsStream)
		{
			this.payloadStream.Dispose();
		}

		this.disposed = true;
	}

	/// <summary>
	///     Asynchronously iterates over each regular file entry inside the payload without buffering to disk.
	/// </summary>
	/// <remarks>
	///     Read each entry's data before advancing to the next iteration in the enumeration.
	/// </remarks>
	public async IAsyncEnumerable<UrpglibPackageEntry> ReadEntriesAsync(
		[EnumeratorCancellation] CancellationToken ct = default)
	{
		ObjectDisposedException.ThrowIf(this.disposed, this);

		using var tarReader = this.OpenPayload();

		while (await tarReader.GetNextEntryAsync(cancellationToken: ct).ConfigureAwait(false) is { } entry)
		{
			if (entry.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile &&
				entry.DataStream is not null)
			{
				yield return new UrpglibPackageEntry(entry.Name, entry.Length, entry.DataStream);
			}
		}
	}

	/// <summary>
	///     Extracts all payload contents into the specified directory on disk.
	/// </summary>
	/// <param name="destinationDirectory">Directory path where files will be written.</param>
	/// <param name="overwrite">Whether to overwrite existing files.</param>
	/// <param name="ct">Cancellation token.</param>
	public async Task ExtractToDirectoryAsync(
		string destinationDirectory,
		bool overwrite = false,
		CancellationToken ct = default)
	{
		ObjectDisposedException.ThrowIf(this.disposed, this);

		var fullDestDir = Path.GetFullPath(destinationDirectory);
		if (!fullDestDir.EndsWith(Path.DirectorySeparatorChar))
		{
			fullDestDir += Path.DirectorySeparatorChar;
		}

		Directory.CreateDirectory(fullDestDir);

		var tarReader = this.OpenPayload();
		await using (tarReader.ConfigureAwait(false))
		{
			while (await tarReader.GetNextEntryAsync(cancellationToken: ct).ConfigureAwait(false) is { } entry)
			{
				var targetPath = Path.GetFullPath(Path.Combine(fullDestDir, entry.Name));

				// Zip/Tar Slip directory traversal defense
				if (!targetPath.StartsWith(fullDestDir, StringComparison.OrdinalIgnoreCase))
				{
					UrpglibFileFormatException.Throw($"Entry '{entry.Name}' resolves outside the target destination.");
				}

				if (entry.EntryType is TarEntryType.Directory)
				{
					Directory.CreateDirectory(targetPath);
					continue;
				}

				if (entry.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile) ||
					entry.DataStream is null)
				{
					continue;
				}

				var dirName = Path.GetDirectoryName(targetPath);
				if (!string.IsNullOrEmpty(dirName))
				{
					Directory.CreateDirectory(dirName);
				}

				var mode = overwrite ? FileMode.Create : FileMode.CreateNew;
				var destFileStream = new FileStream(targetPath, mode, FileAccess.Write, FileShare.None);
				await using (destFileStream.ConfigureAwait(false))
				{
					await entry.DataStream.CopyToAsync(destFileStream, ct)
						.ConfigureAwait(false);
					;
				}
			}
		}
	}

	/// <summary>
	///     Loads all package files into an in-memory dictionary.
	///     Use this when random-access queries on specific files are required.
	/// </summary>
	public async Task<IReadOnlyDictionary<string, byte[]>> ReadAllToMemoryAsync(CancellationToken ct = default)
	{
		ObjectDisposedException.ThrowIf(this.disposed, this);

		var dict = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
		await foreach (var entry in this.ReadEntriesAsync(ct).ConfigureAwait(false))
		{
			dict[entry.Name] = await entry.ReadAsBytesAsync(ct).ConfigureAwait(false);
		}

		return dict;
	}

	/// <summary>
	///     Opens the compressed payload as a TarReader for iterating through the contained files.
	///     The caller is responsible for disposing the TarReader.
	/// </summary>
	/// <returns>A new TarReader instance for the payload.</returns>
	public TarReader OpenPayload()
	{
		ObjectDisposedException.ThrowIf(this.disposed, this);

		if (this.payloadStream.CanSeek)
		{
			this.payloadStream.Position = this.payloadStartPosition;
		}

		var decompressionStream = this.Header.PayloadCompression switch
		{
			PayloadCompressionType.Gzip => new GZipStream(this.payloadStream, CompressionMode.Decompress, true),
			PayloadCompressionType.None => this.payloadStream,
			_ => throw new NotSupportedException(
				$"Payload compression type '{this.Header.PayloadCompression}' is not supported.")
		};

		return new TarReader(decompressionStream);
	}
}
