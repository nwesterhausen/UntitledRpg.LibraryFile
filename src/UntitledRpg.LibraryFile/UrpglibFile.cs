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
///     Provides high-level entry points for inspecting, reading, creating, and validating .urpglib packages.
/// </summary>
public static class UrpglibFile
{
	/// <summary>
	///     Reads only the header and package manifest without decompressing or loading the payload.
	/// </summary>
	public static async Task<PackageManifest> ReadManifestAsync(string packagePath, CancellationToken ct = default)
	{
		var fs = new FileStream(packagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		await using (fs.ConfigureAwait(false))
		{
			var (_, manifest) = await ReadHeaderAndManifestAsync(fs, ct)
				.ConfigureAwait(false);
			return manifest;
		}
	}

	/// <summary>
	///     Reads only the header and package manifest from an existing stream.
	/// </summary>
	public static async Task<PackageManifest> ReadManifestAsync(Stream stream, CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNull(stream);

		var (_, manifest) = await ReadHeaderAndManifestAsync(stream, ct).ConfigureAwait(false);
		return manifest;
	}

	/// <summary>
	///     Opens an existing .urpglib file for forward-only or random payload access.
	/// </summary>
	public static async Task<UrpglibPackage> OpenReadAsync(string packagePath, CancellationToken ct = default)
	{
		var fs = new FileStream(packagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		try
		{
			var (header, manifest) = await ReadHeaderAndManifestAsync(fs, ct).ConfigureAwait(false);
			var payloadOffset = fs.Position;
			return new UrpglibPackage(header, manifest, fs, payloadOffset);
		}
		catch
		{
			await fs.DisposeAsync().ConfigureAwait(false);
			throw;
		}
	}

	/// <summary>
	///     Opens a .urpglib package from a stream.
	/// </summary>
	public static async Task<UrpglibPackage> OpenReadAsync(Stream stream, bool leaveOpen = false,
		CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNull(stream);

		var initialPosition = stream.CanSeek ? stream.Position : 0;
		var (header, manifest) = await ReadHeaderAndManifestAsync(stream, ct).ConfigureAwait(false);
		var payloadOffset = stream.CanSeek
			? stream.Position
			: initialPosition + UrpglibConstants.MinFileSize + header.ManifestLength;
		return new UrpglibPackage(header, manifest, stream, payloadOffset, !leaveOpen);
	}

	/// <summary>
	///     Extracts an entire .urpglib package directly to disk.
	/// </summary>
	public static async Task ExtractToDirectoryAsync(
		string packagePath,
		string destinationDirectory,
		bool overwrite = false,
		CancellationToken ct = default)
	{
		using var package = await OpenReadAsync(packagePath, ct).ConfigureAwait(false);
		await package.ExtractToDirectoryAsync(destinationDirectory, overwrite, ct).ConfigureAwait(false);
	}

	/// <summary>
	///     Creates an archive from a source directory and manifest in a single call.
	/// </summary>
	public static async Task CreateFromDirectoryAsync(
		string sourceDirectory,
		string destinationPackagePath,
		PackageManifest manifest,
		PayloadCompressionType compression = PayloadCompressionType.Gzip,
		CancellationToken ct = default)
	{
		var builder = new UrpglibPackageBuilder()
			.WithManifest(manifest)
			.WithCompression(compression)
			.AddDirectory(sourceDirectory);

		await builder.BuildAsync(destinationPackagePath, ct).ConfigureAwait(false);
	}

	/// <summary>
	///     Validates the archive structure, header magic, schema versions, manifest JSON, and payload integrity.
	/// </summary>
	public static async Task<UrpglibValidationResult> ValidateAsync(
		string packagePath,
		bool validatePayload = true,
		CancellationToken ct = default)
	{
		if (!File.Exists(packagePath))
		{
			return new UrpglibValidationResult { Errors = [$"File does not exist: '{packagePath}'."] };
		}

		var fs = new FileStream(packagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		await using (fs.ConfigureAwait(false))
		{
			return await ValidateAsync(fs, validatePayload, ct).ConfigureAwait(false);
		}
	}

	/// <summary>
	///     Validates the archive structure directly from a seekable stream.
	/// </summary>
	public static async Task<UrpglibValidationResult> ValidateAsync(
		Stream stream,
		bool validatePayload = true,
		CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNull(stream);

		var errors = new List<string>();
		UrpglibHeader? header = null;
		PackageManifest? manifest = null;
		var entryCount = 0;

		var startPos = stream.CanSeek ? stream.Position : 0;

		// 1. Minimum size check
		if (stream.CanSeek && stream.Length - stream.Position < UrpglibConstants.MinFileSize)
		{
			errors.Add($"File size is below the minimum required header size ({UrpglibConstants.MinFileSize} bytes).");
			return new UrpglibValidationResult { Errors = errors };
		}

		// 2. Header parsing
		var headerBuffer = new byte[UrpglibConstants.MinFileSize];
		var bytesRead = await stream.ReadAsync(headerBuffer.AsMemory(0, (int)UrpglibConstants.MinFileSize), ct)
			.ConfigureAwait(false);
		if (bytesRead < UrpglibConstants.MinFileSize)
		{
			errors.Add("Failed to read complete 18-byte header.");
			return new UrpglibValidationResult { Errors = errors };
		}

		if (!headerBuffer.AsSpan(0, 8).SequenceEqual(UrpglibConstants.MagicBytes))
		{
			errors.Add("Invalid magic bytes signature. Not a valid .urpglib file.");
		}

		var headerSchema = headerBuffer[8];
		var manifestSchema = BitConverter.ToUInt16(headerBuffer, 9);
		var compressionByte = headerBuffer[11];
		var payloadSchema = BitConverter.ToUInt16(headerBuffer, 12);
		var manifestLength = BitConverter.ToUInt32(headerBuffer, 14);

		if (!Enum.IsDefined(typeof(PayloadCompressionType), (int)compressionByte))
		{
			errors.Add($"Unsupported compression type value '0x{compressionByte:X2}'.");
		}

		header = new UrpglibHeader
		{
			HeaderSchemaVersion = headerSchema,
			ManifestSchemaVersion = manifestSchema,
			PayloadCompression = (PayloadCompressionType)compressionByte,
			PayloadSchemaVersion = payloadSchema,
			ManifestLength = manifestLength
		};

		if (header.HeaderSchemaVersion > UrpglibConstants.CurrentHeaderSchemaVersion)
		{
			errors.Add(
				$"Unsupported header schema version: {header.HeaderSchemaVersion} (supported: <= {UrpglibConstants.CurrentHeaderSchemaVersion}).");
		}

		if (header.ManifestLength == 0)
		{
			errors.Add("Manifest length cannot be zero.");
		}

		// 3. Manifest read & deserialize
		if (header.ManifestLength > 0)
		{
			if (stream.CanSeek && stream.Length - stream.Position < header.ManifestLength)
			{
				errors.Add($"Stream truncated before manifest finished (expected {header.ManifestLength} bytes).");
			}
			else
			{
				var manifestBytes = new byte[header.ManifestLength];
				var readManifest = await stream.ReadAsync(manifestBytes.AsMemory(0, (int)header.ManifestLength), ct)
					.ConfigureAwait(false);

				if (readManifest < header.ManifestLength)
				{
					errors.Add("Failed to read the complete manifest buffer.");
				}
				else
				{
					try
					{
						manifest = JsonSerializer.Deserialize<PackageManifest>(
							manifestBytes,
							UrpglibConstants.DefaultJsonSerializerOptions);

						if (manifest is null)
						{
							errors.Add("Package manifest deserialized to null.");
						}
					}
					catch (JsonException ex)
					{
						errors.Add($"Manifest JSON deserialization error: {ex.Message}");
					}
				}
			}
		}

		// 4. Payload integrity check
		if (validatePayload && errors.Count == 0)
		{
			try
			{
				var decompressionStream = header.PayloadCompression switch
				{
					PayloadCompressionType.Gzip => new GZipStream(stream, CompressionMode.Decompress, true),
					PayloadCompressionType.None => stream,
					_ => throw new NotSupportedException()
				};

				var tarReader = new TarReader(decompressionStream);
				await using (tarReader.ConfigureAwait(false))
				{
					while (await tarReader.GetNextEntryAsync(cancellationToken: ct).ConfigureAwait(false) is { } entry)
					{
						if (entry.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile)
						{
							entryCount++;
						}
					}
				}
			}
			catch (InvalidDataException ex)
			{
				errors.Add($"Corrupt payload data: {ex.Message}");
			}
			catch (IOException ex)
			{
				errors.Add($"Payload read error or truncated stream: {ex.Message}");
			}
			catch (NotSupportedException ex)
			{
				errors.Add($"Unsupported payload format: {ex.Message}");
			}
		}

		// Restore position if possible
		if (stream.CanSeek)
		{
			stream.Position = startPos;
		}

		return new UrpglibValidationResult
		{
			Errors = errors,
			Header = header,
			Manifest = manifest,
			EntryCount = entryCount
		};
	}

	private static async Task<(UrpglibHeader Header, PackageManifest Manifest)> ReadHeaderAndManifestAsync(
		Stream stream,
		CancellationToken ct)
	{
		UrpglibFileSizeException.ThrowIf(
			stream.CanSeek && stream.Length - stream.Position < UrpglibConstants.MinFileSize,
			"File is smaller than the minimum required header size.");

		var headerBuffer = new byte[UrpglibConstants.MinFileSize];
		await stream.ReadExactlyAsync(headerBuffer.AsMemory(0, (int)UrpglibConstants.MinFileSize), ct)
			.ConfigureAwait(false);

		if (!headerBuffer.AsSpan(0, 8).SequenceEqual(UrpglibConstants.MagicBytes))
		{
			UrpglibInvalidSignatureException.Throw();
		}

		var header = new UrpglibHeader
		{
			HeaderSchemaVersion = headerBuffer[8],
			ManifestSchemaVersion = BitConverter.ToUInt16(headerBuffer, 9),
			PayloadCompression = (PayloadCompressionType)headerBuffer[11],
			PayloadSchemaVersion = BitConverter.ToUInt16(headerBuffer, 12),
			ManifestLength = BitConverter.ToUInt32(headerBuffer, 14)
		};

		UrpglibFileFormatException.ThrowIf(
			header.ManifestLength == 0,
			"Package manifest length cannot be zero.");

		var manifestBuffer = new byte[header.ManifestLength];
		await stream.ReadExactlyAsync(manifestBuffer.AsMemory(0, (int)header.ManifestLength), ct)
			.ConfigureAwait(false);

		PackageManifest manifest;
		try
		{
			manifest = JsonSerializer.Deserialize<PackageManifest>(
						   manifestBuffer,
						   UrpglibConstants.DefaultJsonSerializerOptions)
					   ?? throw new UrpglibManifestDeserializationException();
		}
		catch (JsonException ex)
		{
			throw new UrpglibManifestDeserializationException(ex);
		}

		return (header, manifest);
	}
}
