using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UntitledRpgLogic.LibraryFile;

/// <summary>
///     Handles writing .urpglib files, including header, manifest, and payload.
/// </summary>
public static class UrpglibWriter
{
	/// <summary>
	///     Creates a .urpglib file from a manifest and a collection of data files.
	/// </summary>
	/// <param name="outputPath">The path to write the .urpglib file.</param>
	/// <param name="manifest">The package manifest data.</param>
	/// <param name="files">A dictionary where the key is the path inside the archive and the value is the file content.</param>
	/// <param name="compressionType">The compression to use for the payload.</param>
	public static async Task WriteAsync(string outputPath, PackageManifest manifest,
		IReadOnlyDictionary<string, byte[]> files, PayloadCompressionType compressionType = PayloadCompressionType.Gzip)
	{
		// 1. Create the compressed payload in memory.
		byte[] payloadBytes;
		var payloadStream = new MemoryStream();
		await using (payloadStream.ConfigureAwait(false))
		{
			switch (compressionType)
			{
				case PayloadCompressionType.Gzip:
					{
						var gzipStream = new GZipStream(payloadStream, CompressionMode.Compress, true);
						await using (gzipStream.ConfigureAwait(false))
						{
							await WriteTarArchiveAsync(gzipStream, files).ConfigureAwait(false);
						}

						break;
					}
				case PayloadCompressionType.None:
					await WriteTarArchiveAsync(payloadStream, files).ConfigureAwait(false);
					break;
				default:
					throw new NotSupportedException(
						$"Compression type '{compressionType}' is not supported for writing.");
			}

			payloadBytes = payloadStream.ToArray();
		}

		// 2. Serialize manifest to JSON
		var manifestJsonBytes =
			JsonSerializer.SerializeToUtf8Bytes(manifest, UrpglibConstants.DefaultJsonSerializerOptions);

		// 3. Write the final .urpglib file
		var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
		await using var stream = fileStream.ConfigureAwait(false);
		var writer = new BinaryWriter(fileStream, Encoding.UTF8, false);
		await using var writer1 = writer.ConfigureAwait(false);

		// -- Write Header --
		writer.Write(UrpglibConstants.MagicBytes);
		writer.Write(UrpglibConstants.CurrentHeaderSchemaVersion);
		writer.Write(UrpglibConstants.CurrentManifestSchemaVersion);
		writer.Write((byte)compressionType);
		writer.Write(UrpglibConstants.CurrentPayloadSchemaVersion);
		writer.Write((uint)manifestJsonBytes.Length);

		// -- Write Manifest --
		writer.Write(manifestJsonBytes);

		// -- Write Payload --
		writer.Write(payloadBytes);
	}

	private static async Task WriteTarArchiveAsync(Stream targetStream, IReadOnlyDictionary<string, byte[]>? files)
	{
		if (files is null or { Count: 0 })
		{
			return;
		}

		var tarWriter = new TarWriter(targetStream, true);
		await using (tarWriter.ConfigureAwait(false))
		{
			foreach (var file in files)
			{
				using var dataStream = new MemoryStream(file.Value);
				var tarEntry = new PaxTarEntry(TarEntryType.RegularFile, file.Key) { DataStream = dataStream };
				await tarWriter.WriteEntryAsync(tarEntry).ConfigureAwait(false);
			}
		}
	}
}
