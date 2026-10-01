using System.Globalization;
using System.Text;
using System.Text.Json;

namespace UntitledRpg.LibraryFile.Tests;

public static class HelperMethods
{
	private const string TEST_ULID_BASE32 = "01ARZ3NDEKTSV4RRFFQ69G5FAV";

	public static PackageManifest CreateTestManifest() => new()
	{
		// Explicitly set to test serialization
		Id = Ulid.Parse(TEST_ULID_BASE32, CultureInfo.InvariantCulture),
		Name = "Test Package",
		AuthorName = "Test Author",
		Description = "A test package for unit testing",
		Version = new Version("1.2.3"),
		Dependencies =
		[
			new PackageLink(Ulid.NewUlid(), "1"),
			new PackageLink(Ulid.NewUlid(), "2")
		]
	};

	public static async Task CreateValidUrpglibFile(string filePath, PackageManifest manifest,
		PayloadCompressionType compression = PayloadCompressionType.None)
	{
		var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
		await using var stream = fileStream.ConfigureAwait(false);
		var writer = new BinaryWriter(fileStream, Encoding.UTF8);
		await using var writer1 = writer.ConfigureAwait(false);

		// Write magic bytes
		writer.Write(UrpglibConstants.MagicBytes);

		// Write header
		writer.Write(UrpglibConstants.CurrentHeaderSchemaVersion);
		writer.Write(UrpglibConstants.CurrentManifestSchemaVersion);
		writer.Write((byte)compression);
		writer.Write(UrpglibConstants.CurrentPayloadSchemaVersion);

		// Serialize manifest
		var manifestJson = JsonSerializer.SerializeToUtf8Bytes(manifest, UrpglibConstants.DefaultJsonSerializerOptions);

		writer.Write((uint)manifestJson.Length);
		writer.Write(manifestJson);

		// Write dummy payload
		var dummyPayload = Encoding.UTF8.GetBytes("dummy payload content");
		writer.Write(dummyPayload);
	}

	public static async Task CreateUrpglibFileWithVersion(string filePath, PackageManifest manifest,
		byte headerVersion)
	{
		var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
		await using var stream = fileStream.ConfigureAwait(false);
		var writer = new BinaryWriter(fileStream, Encoding.UTF8);
		await using var writer1 = writer.ConfigureAwait(false);

		// Write magic bytes
		writer.Write(UrpglibConstants.MagicBytes);

		// Write header with custom version
		writer.Write(headerVersion);
		writer.Write(UrpglibConstants.CurrentManifestSchemaVersion);
		writer.Write((byte)PayloadCompressionType.None);
		writer.Write(UrpglibConstants.CurrentPayloadSchemaVersion);

		// Serialize manifest
		var manifestJson = JsonSerializer.SerializeToUtf8Bytes(manifest, UrpglibConstants.DefaultJsonSerializerOptions);

		writer.Write((uint)manifestJson.Length);
		writer.Write(manifestJson);

		// Write dummy payload
		var dummyPayload = Encoding.UTF8.GetBytes("dummy payload content");
		writer.Write(dummyPayload);
	}

	public static async Task CreateUrpglibFileWithInvalidManifest(string filePath)
	{
		var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
		await using var stream = fileStream.ConfigureAwait(false);
		var writer = new BinaryWriter(fileStream, Encoding.UTF8);
		await using var writer1 = writer.ConfigureAwait(false);

		// Write magic bytes
		writer.Write(UrpglibConstants.MagicBytes);

		// Write header
		writer.Write(UrpglibConstants.CurrentHeaderSchemaVersion);
		writer.Write(UrpglibConstants.CurrentManifestSchemaVersion);
		writer.Write((byte)PayloadCompressionType.None);
		writer.Write(UrpglibConstants.CurrentPayloadSchemaVersion);

		// Write invalid JSON
		var invalidJson = Encoding.UTF8.GetBytes("{ invalid json content }");
		writer.Write((uint)invalidJson.Length);
		writer.Write(invalidJson);

		// Write dummy payload
		var dummyPayload = Encoding.UTF8.GetBytes("dummy payload content");
		writer.Write(dummyPayload);
	}

	public static async Task CreateUrpglibFileWithManifestLength(string filePath, uint manifestLength)
	{
		var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
		await using var stream = fileStream.ConfigureAwait(false);
		var writer = new BinaryWriter(fileStream, Encoding.UTF8);
		await using var writer1 = writer.ConfigureAwait(false);

		// Write magic bytes
		writer.Write(UrpglibConstants.MagicBytes);

		// Write header
		writer.Write(UrpglibConstants.CurrentHeaderSchemaVersion);
		writer.Write(UrpglibConstants.CurrentManifestSchemaVersion);
		writer.Write((byte)PayloadCompressionType.None);
		writer.Write(UrpglibConstants.CurrentPayloadSchemaVersion);

		// Write custom manifest length
		writer.Write(manifestLength);

		// If manifest length is not zero, the file will become invalid because there is no actual manifest data written.
	}
}
