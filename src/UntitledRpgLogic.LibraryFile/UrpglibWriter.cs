using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace UntitledRpgLogic.LibraryFile;

/// <summary>
///		Handles writing .urpglib files, including header, manifest, and payload.
/// </summary>
public static class UrpglibWriter
{

	/// <summary>
	/// Creates a .urpglib file from a manifest and a collection of data files.
	/// </summary>
	/// <param name="outputPath">The path to write the .urpglib file.</param>
	/// <param name="manifest">The package manifest data.</param>
	/// <param name="files">A dictionary where the key is the path inside the archive and the value is the file content.</param>
	/// <param name="compressionType">The compression to use for the payload.</param>
	public static async Task WriteAsync(string outputPath, PackageManifest manifest, IReadOnlyDictionary<string, byte[]> files, PayloadCompressionType compressionType = PayloadCompressionType.Gzip)
	{
		// 1. Create the compressed payload in memory.
		var payloadStream = new MemoryStream();

		Stream compressionStream = compressionType switch
		{
			PayloadCompressionType.Gzip => new GZipStream(payloadStream, CompressionMode.Compress, leaveOpen: true),
			PayloadCompressionType.None => payloadStream // No compression, write directly to memory stream
			,
			_ => throw new NotSupportedException($"Compression type '{compressionType}' is not supported for writing.")
		};

		// Leave the following check in place, because the signature doesn't allow null but this is a library method.
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (files is not null)
		{
			await using (compressionStream.ConfigureAwait(false))
			{
				var tarWriter = new TarWriter(compressionStream);
				await using (tarWriter.ConfigureAwait(false))
				{
					foreach (var file in files)
					{
						var tarEntry = new PaxTarEntry(TarEntryType.RegularFile, file.Key) { DataStream = new MemoryStream(file.Value) };
						await tarWriter.WriteEntryAsync(tarEntry).ConfigureAwait(false);
					}
				}
			}
		}

		await compressionStream.FlushAsync().ConfigureAwait(false);

		var payloadBytes = payloadStream.ToArray();

		await payloadStream.DisposeAsync().ConfigureAwait(false);
		await compressionStream.DisposeAsync().ConfigureAwait(false);

		// 2. Serialize manifest to JSON
		var manifestJsonBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, UrpglibConstants.DefaultJsonSerializerOptions);

		// 3. Write the final .urpglib file
		var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
		await using var stream = fileStream.ConfigureAwait(false);
		var writer = new BinaryWriter(fileStream, Encoding.UTF8, leaveOpen: false);
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
}
