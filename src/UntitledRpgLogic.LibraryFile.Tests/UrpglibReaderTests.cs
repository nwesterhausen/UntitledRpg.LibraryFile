using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CA1707 // Naming rules disallow underscores in identifiers

namespace UntitledRpgLogic.LibraryFile.Tests;

[TestClass]
#pragma warning disable CA1515
public sealed class UrpglibReaderTests : IDisposable
#pragma warning restore CA1515
{
	private readonly string tempDirectory;

	public void Dispose()
	{
		if (Directory.Exists(this.tempDirectory))
		{
			Directory.Delete(this.tempDirectory, true);
		}
	}

	[TestMethod]
	public async Task ReadAsync_ValidFileWithMemoryStream_ReturnsPackageSuccessfully()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "test.urpglib");
		var manifest = CreateTestManifest();
		await CreateValidUrpglibFile(filePath, manifest).ConfigureAwait(true);

		// Act
		var package = await UrpglibReader.ReadAsync(filePath, readPayloadIntoMemory: true).ConfigureAwait(true);

		// Assert
		using (package)
		{
			Assert.IsNotNull(package);
			Assert.IsNotNull(package.Header);
			Assert.IsNotNull(package.Manifest);
			Assert.AreEqual(manifest.Name, package.Manifest.Name);
			Assert.AreEqual(manifest.AuthorName, package.Manifest.AuthorName);
			Assert.AreEqual(UrpglibConstants.CurrentHeaderSchemaVersion, package.Header.HeaderSchemaVersion);
		}
	}

	[TestMethod]
	public async Task ReadAsync_ValidFileWithFileStream_ReturnsPackageSuccessfully()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "test.urpglib");
		var manifest = CreateTestManifest();
		await CreateValidUrpglibFile(filePath, manifest).ConfigureAwait(false);

		// Act
		var package = await UrpglibReader.ReadAsync(filePath, readPayloadIntoMemory: false).ConfigureAwait(false);

		// Assert
		using (package)
		{
			Assert.IsNotNull(package);
			Assert.IsNotNull(package.Header);
			Assert.IsNotNull(package.Manifest);
			Assert.AreEqual(manifest.Name, package.Manifest.Name);
			Assert.AreEqual(manifest.AuthorName, package.Manifest.AuthorName);
		}
	}

	[TestMethod]
	public async Task ReadAsync_FileWithInvalidMagicBytes_ThrowsUrpglibFileFormatException()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "invalid.urpglib");
		await File.WriteAllBytesAsync(filePath, [
			0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00
		]).ConfigureAwait(false);

		// Act & Assert
		var exception = await Assert.ThrowsExactlyAsync<UrpglibFileFormatException>(
			() => UrpglibReader.ReadAsync(filePath)).ConfigureAwait(false);

		Assert.IsNotNull(exception);
		Assert.IsTrue(exception.Message.Contains("Invalid file signature", StringComparison.InvariantCultureIgnoreCase));
	}

	[TestMethod]
	public async Task ReadAsync_FileWithNewerHeaderVersion_ThrowsUrpglibVersionMismatchException()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "newer_version.urpglib");
		var manifest = CreateTestManifest();
		await CreateUrpglibFileWithVersion(filePath, manifest, UrpglibConstants.CurrentHeaderSchemaVersion + 1).ConfigureAwait(false);

		// Act & Assert
		var exception = await Assert.ThrowsExactlyAsync<UrpglibVersionMismatchException>(
			() => UrpglibReader.ReadAsync(filePath)).ConfigureAwait(false);

		Assert.IsNotNull(exception);
		Assert.IsTrue(exception.Message.Contains("newer than supported version", StringComparison.InvariantCultureIgnoreCase));
	}

	[TestMethod]
	public async Task ReadAsync_FileWithInvalidManifest_ThrowsUrpglibFileFormatException()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "invalid_manifest.urpglib");
		await CreateUrpglibFileWithInvalidManifest(filePath).ConfigureAwait(false);

		// Act & Assert
		var exception = await Assert.ThrowsExactlyAsync<UrpglibFileFormatException>(
			() => UrpglibReader.ReadAsync(filePath)).ConfigureAwait(false);

		Assert.IsNotNull(exception);
		Assert.IsTrue(exception.Message.Contains("Failed to deserialize package manifest", StringComparison.InvariantCultureIgnoreCase));
	}

	[TestMethod]
	public async Task ReadAsync_EmptyFile_ThrowsException()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "empty.urpglib");
		await File.WriteAllBytesAsync(filePath, []).ConfigureAwait(false);

		// Act & Assert
		_ = await Assert.ThrowsExactlyAsync<UrpglibFileSizeException>(
			() => UrpglibReader.ReadAsync(filePath)).ConfigureAwait(false);
	}

	[TestMethod]
	public async Task ReadAsync_FileWithGzipCompression_ReadsCorrectly()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "gzip_test.urpglib");
		var manifest = CreateTestManifest();
		await CreateValidUrpglibFile(filePath, manifest, PayloadCompressionType.Gzip).ConfigureAwait(false);

		// Act
		var package = await UrpglibReader.ReadAsync(filePath).ConfigureAwait(false);

		// Assert
		using (package)
		{
			Assert.AreEqual(PayloadCompressionType.Gzip, package.Header.PayloadCompression);
			Assert.IsNotNull(package.Manifest);
			Assert.AreEqual(manifest.Name, package.Manifest.Name);
		}
	}

	[TestMethod]
	public async Task ReadAsync_FileWithNoCompression_ReadsCorrectly()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "no_compression_test.urpglib");
		var manifest = CreateTestManifest();
		await CreateValidUrpglibFile(filePath, manifest, PayloadCompressionType.None).ConfigureAwait(false);

		// Act
		var package = await UrpglibReader.ReadAsync(filePath).ConfigureAwait(false);

		// Assert
		using (package)
		{
			Assert.AreEqual(PayloadCompressionType.None, package.Header.PayloadCompression);
			Assert.IsNotNull(package.Manifest);
			Assert.AreEqual(manifest.Name, package.Manifest.Name);
		}
	}

	[TestMethod]
	public async Task ReadAsync_FileWithLargeManifest_ReadsCorrectly()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "large_manifest.urpglib");
		var manifest = CreateTestManifest();
		manifest.Description = new string('A', 10000); // Large description
		await CreateValidUrpglibFile(filePath, manifest).ConfigureAwait(false);

		// Act
		var package = await UrpglibReader.ReadAsync(filePath).ConfigureAwait(false);

		// Assert
		using (package)
		{
			Assert.IsNotNull(package.Manifest);
			Assert.AreEqual(10000, package.Manifest.Description.Length);
			Assert.AreEqual(manifest.Name, package.Manifest.Name);
		}
	}

	[TestMethod]
	public async Task ReadAsync_NonExistentFile_ThrowsFileNotFoundException()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "nonexistent.urpglib");

		// Act & Assert
		_ = await Assert.ThrowsExactlyAsync<FileNotFoundException>(
			() => UrpglibReader.ReadAsync(filePath)).ConfigureAwait(false);
	}

	[TestMethod]
	public async Task ReadAsync_TruncatedFile_ThrowsEndOfStreamException()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "truncated.urpglib");
		// The file actually doesn't contain the full manifest data, simulating a truncated file
		await CreateUrpglibFileWithManifestLength(filePath, 10).ConfigureAwait(false);

		// Act & Assert
		_ = await Assert.ThrowsExactlyAsync<UrpglibFileFormatException>(
			() => UrpglibReader.ReadAsync(filePath)).ConfigureAwait(false);
	}

	[TestMethod]
	public async Task ReadAsync_FileWithZeroManifestLength_ThrowsFileFormatException()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "zero_manifest.urpglib");
		await CreateUrpglibFileWithManifestLength(filePath, 0).ConfigureAwait(false);

		// Act & Assert
		_ = await Assert.ThrowsExactlyAsync<UrpglibFileFormatException>(
			() => UrpglibReader.ReadAsync(filePath)).ConfigureAwait(false);
	}

	[TestMethod]
	public async Task ReadAsync_MultipleConcurrentReads_AllSucceed()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "concurrent_test.urpglib");
		var manifest = CreateTestManifest();
		await CreateValidUrpglibFile(filePath, manifest).ConfigureAwait(false);

		// Act
		var tasks = Enumerable.Range(0, 10).Select(async _ =>
		{
			var package = await UrpglibReader.ReadAsync(filePath, readPayloadIntoMemory: true).ConfigureAwait(false);
			using (package)
			{
				Assert.IsNotNull(package.Manifest);
				return package.Manifest.Name;
			}
		});

		var results = await Task.WhenAll(tasks).ConfigureAwait(false);

		// Assert
		Assert.AreEqual(10, results.Length);
		foreach (var result in results)
		{
			Assert.AreEqual(manifest.Name, result);
		}
	}

	#region Helper Methods

	private static PackageManifest CreateTestManifest()
	{
		return new PackageManifest
		{
			Name = "Test Package",
			AuthorName = "Test Author",
			Description = "A test package for unit testing",
			Version = "1.2.3"
		};
	}

	private static async Task CreateValidUrpglibFile(string filePath, PackageManifest manifest, PayloadCompressionType compression = PayloadCompressionType.None)
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

	private static async Task CreateUrpglibFileWithVersion(string filePath, PackageManifest manifest, byte headerVersion)
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

	private static async Task CreateUrpglibFileWithInvalidManifest(string filePath)
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

	private static async Task CreateUrpglibFileWithManifestLength(string filePath, uint manifestLength)
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

	public UrpglibReaderTests()
	{
		this.tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		_ = Directory.CreateDirectory(this.tempDirectory);
	}

	#endregion
}
