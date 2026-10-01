using System.Text;
using System.Text.Json;

namespace UntitledRpg.LibraryFile.Tests;

public class UrpglibFileTests
{
	private readonly string tempDirectory;

	public UrpglibFileTests()
	{
		this.tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		_ = Directory.CreateDirectory(this.tempDirectory);
	}

	[TestMethod]
	public async Task UrpglibFile_ReadManifestAsync_FromFileAndStream_ReturnsManifestQuickly()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "manifest_peek_test.urpglib");
		var manifest = HelperMethods.CreateTestManifest();
		await HelperMethods.CreateValidUrpglibFile(filePath, manifest).ConfigureAwait(false);

		// Act - From file path
		var resultFromFile = await UrpglibFile.ReadManifestAsync(filePath).ConfigureAwait(false);

		// Act - From stream
		var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		PackageManifest resultFromStream;
		await using (fs.ConfigureAwait(false))
		{
			resultFromStream = await UrpglibFile.ReadManifestAsync(fs).ConfigureAwait(false);
		}

		// Assert
		Assert.IsNotNull(resultFromFile);
		Assert.AreEqual(manifest.Name, resultFromFile.Name);
		Assert.AreEqual(manifest.AuthorName, resultFromFile.AuthorName);
		Assert.AreEqual(manifest.Id, resultFromFile.Id);

		Assert.IsNotNull(resultFromStream);
		Assert.AreEqual(manifest.Name, resultFromStream.Name);
		Assert.AreEqual(manifest.Id, resultFromStream.Id);
	}

	[TestMethod]
	public async Task UrpglibFile_OpenReadAsync_StreamsMultipleEntries()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "open_read_test.urpglib");
		var manifest = HelperMethods.CreateTestManifest();
		var files = new Dictionary<string, byte[]>
		{
			["monsters/goblin.toml"] = "health = 25\nattack = 5"u8.ToArray(),
			["monsters/orc.toml"] = "health = 80\nattack = 18"u8.ToArray()
		};
		await UrpglibWriter.WriteAsync(filePath, manifest, files).ConfigureAwait(false);

		// Act
		var package = await UrpglibFile.OpenReadAsync(filePath).ConfigureAwait(false);
		var readFiles = new Dictionary<string, string>();

		await using (package.ConfigureAwait(false))
		{
			await foreach (var entry in package.ReadEntriesAsync().ConfigureAwait(false))
			{
				readFiles[entry.Name] = await entry.ReadAsStringAsync().ConfigureAwait(false);
			}
		}

		// Assert
		Assert.HasCount(2, readFiles);
		Assert.IsTrue(readFiles.ContainsKey("monsters/goblin.toml"));
		Assert.IsTrue(readFiles["monsters/goblin.toml"].Contains("health = 25", StringComparison.Ordinal));
		Assert.IsTrue(readFiles.ContainsKey("monsters/orc.toml"));
	}

	[TestMethod]
	public async Task UrpglibPackage_ReadAllToMemoryAsync_ReturnsAllEntries()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "read_all_memory.urpglib");
		var manifest = HelperMethods.CreateTestManifest();
		var files = new Dictionary<string, byte[]>
		{
			["data/items.json"] = "{\"potion\": 10}"u8.ToArray(),
			["data/spells.json"] = "{\"fireball\": 50}"u8.ToArray()
		};
		await UrpglibWriter.WriteAsync(filePath, manifest, files).ConfigureAwait(false);

		// Act
		var package = await UrpglibFile.OpenReadAsync(filePath).ConfigureAwait(false);
		IReadOnlyDictionary<string, byte[]> inMemoryMap;
		await using (package.ConfigureAwait(false))
		{
			inMemoryMap = await package.ReadAllToMemoryAsync().ConfigureAwait(false);
		}

		// Assert
		Assert.HasCount(2, inMemoryMap);
		Assert.IsTrue(inMemoryMap.ContainsKey("data/items.json"));
		CollectionAssert.AreEqual("{\"potion\": 10}"u8.ToArray(), inMemoryMap["data/items.json"].ToArray());
	}

	[TestMethod]
	public async Task UrpglibPackage_ExtractToDirectoryAsync_ExtractsNestedFilesSuccessfully()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "extract_test.urpglib");
		var extractDir = Path.Combine(this.tempDirectory, "extracted_output");
		var manifest = HelperMethods.CreateTestManifest();
		var files = new Dictionary<string, byte[]>
		{
			["subfolder/child.txt"] = "Child content"u8.ToArray(),
			["root.txt"] = "Root content"u8.ToArray()
		};
		await UrpglibWriter.WriteAsync(filePath, manifest, files).ConfigureAwait(false);

		// Act
		await UrpglibFile.ExtractToDirectoryAsync(filePath, extractDir, true).ConfigureAwait(false);

		// Assert
		var childPath = Path.Combine(extractDir, "subfolder", "child.txt");
		var rootPath = Path.Combine(extractDir, "root.txt");

		Assert.IsTrue(File.Exists(childPath));
		Assert.IsTrue(File.Exists(rootPath));
		Assert.AreEqual("Child content", await File.ReadAllTextAsync(childPath).ConfigureAwait(false));
		Assert.AreEqual("Root content", await File.ReadAllTextAsync(rootPath).ConfigureAwait(false));
	}

	[TestMethod]
	public async Task UrpgPackageBuilder_BuildAsync_CreatesValidPackageWithRawBytesAndDiskFiles()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "builder_output.urpglib");
		var manifest = HelperMethods.CreateTestManifest();

		var externalDiskFile = Path.Combine(this.tempDirectory, "on_disk.toml");
		await File.WriteAllTextAsync(externalDiskFile, "name = \"DiskItem\"").ConfigureAwait(false);

		// Act
		var builder = new UrpglibPackageBuilder()
			.WithManifest(manifest)
			.WithCompression(PayloadCompressionType.Gzip)
			.AddFile(externalDiskFile, "items/disk_item.toml")
			.AddBytes("name = \"MemoryItem\""u8.ToArray(), "items/memory_item.toml");

		await builder.BuildAsync(filePath).ConfigureAwait(false);

		// Assert
		var package = await UrpglibFile.OpenReadAsync(filePath).ConfigureAwait(false);
		await using (package.ConfigureAwait(false))
		{
			Assert.IsNotNull(package.Manifest);
			Assert.AreEqual(manifest.Name, package.Manifest.Name);
			Assert.AreEqual(PayloadCompressionType.Gzip, package.Header.PayloadCompression);

			var memoryMap = await package.ReadAllToMemoryAsync().ConfigureAwait(false);
			Assert.HasCount(2, memoryMap);
			Assert.IsTrue(memoryMap.ContainsKey("items/disk_item.toml"));
			Assert.IsTrue(memoryMap.ContainsKey("items/memory_item.toml"));
			Assert.AreEqual("name = \"DiskItem\"", Encoding.UTF8.GetString(memoryMap["items/disk_item.toml"]));
		}
	}

	[TestMethod]
	public async Task UrpglibFile_CreateFromDirectoryAsync_And_Extract_RoundTripsSuccessfully()
	{
		// Arrange
		var sourceDir = Path.Combine(this.tempDirectory, "pack_source");
		var subDir = Path.Combine(sourceDir, "sub");
		Directory.CreateDirectory(subDir);

		await File.WriteAllTextAsync(Path.Combine(sourceDir, "a.txt"), "A").ConfigureAwait(false);
		await File.WriteAllTextAsync(Path.Combine(subDir, "b.txt"), "B").ConfigureAwait(false);

		var archivePath = Path.Combine(this.tempDirectory, "directory_pack.urpglib");
		var targetUnpackDir = Path.Combine(this.tempDirectory, "pack_unpacked");
		var manifest = HelperMethods.CreateTestManifest();

		// Act
		await UrpglibFile.CreateFromDirectoryAsync(sourceDir, archivePath, manifest).ConfigureAwait(false);
		await UrpglibFile.ExtractToDirectoryAsync(archivePath, targetUnpackDir).ConfigureAwait(false);

		// Assert
		Assert.IsTrue(File.Exists(Path.Combine(targetUnpackDir, "a.txt")));
		Assert.IsTrue(File.Exists(Path.Combine(targetUnpackDir, "sub", "b.txt")));
		Assert.AreEqual("A", await File.ReadAllTextAsync(Path.Combine(targetUnpackDir, "a.txt")).ConfigureAwait(false));
		Assert.AreEqual("B",
			await File.ReadAllTextAsync(Path.Combine(targetUnpackDir, "sub", "b.txt")).ConfigureAwait(false));
	}

	[TestMethod]
	public async Task UrpglibFile_ValidateAsync_ValidFile_ReturnsIsValidTrueWithEntryCount()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "validate_valid.urpglib");
		var manifest = HelperMethods.CreateTestManifest();
		var files = new Dictionary<string, byte[]>
		{
			["entry1.toml"] = "a = 1"u8.ToArray(),
			["entry2.toml"] = "b = 2"u8.ToArray(),
			["entry3.toml"] = "c = 3"u8.ToArray()
		};
		await UrpglibWriter.WriteAsync(filePath, manifest, files).ConfigureAwait(false);

		// Act
		var result = await UrpglibFile.ValidateAsync(filePath).ConfigureAwait(false);

		// Assert
		Assert.IsTrue(result.IsValid);
		Assert.IsEmpty(result.Errors);
		Assert.IsNotNull(result.Header);
		Assert.IsNotNull(result.Manifest);
		Assert.AreEqual(manifest.Name, result.Manifest.Name);
		Assert.AreEqual(3, result.EntryCount);
	}

	[TestMethod]
	public async Task UrpglibFile_ValidateAsync_CorruptMagicBytes_ReturnsIsValidFalse()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "validate_corrupt_magic.urpglib");
		await File.WriteAllBytesAsync(filePath, [
			0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
			0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
			0x00, 0x00
		]).ConfigureAwait(false);

		// Act
		var result = await UrpglibFile.ValidateAsync(filePath).ConfigureAwait(false);

		// Assert
		Assert.IsFalse(result.IsValid);
		Assert.Contains(e => e.Contains("Invalid magic bytes signature", StringComparison.OrdinalIgnoreCase),
			result.Errors);
	}

	[TestMethod]
	public async Task UrpglibFile_ValidateAsync_CorruptPayloadData_ReturnsIsValidFalse()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "corrupt_payload.urpglib");
		var manifest = HelperMethods.CreateTestManifest();

		var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
		await using (fileStream.ConfigureAwait(false))
		{
			var writer = new BinaryWriter(fileStream, Encoding.UTF8);
			await using (writer.ConfigureAwait(false))
			{
				writer.Write(UrpglibConstants.MagicBytes);
				writer.Write(UrpglibConstants.CurrentHeaderSchemaVersion);
				writer.Write(UrpglibConstants.CurrentManifestSchemaVersion);
				writer.Write((byte)PayloadCompressionType.Gzip);
				writer.Write(UrpglibConstants.CurrentPayloadSchemaVersion);

				var manifestJson =
					JsonSerializer.SerializeToUtf8Bytes(manifest, UrpglibConstants.DefaultJsonSerializerOptions);
				writer.Write((uint)manifestJson.Length);
				writer.Write(manifestJson);

				// Append non-Gzip garbage bytes for the payload
				writer.Write(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03 });
			}
		}

		// Act
		var result = await UrpglibFile.ValidateAsync(filePath).ConfigureAwait(false);

		// Assert
		Assert.IsFalse(result.IsValid);
		Assert.Contains(e => e.Contains("Corrupt payload data", StringComparison.OrdinalIgnoreCase)
							 || e.Contains("Payload read error", StringComparison.OrdinalIgnoreCase), result.Errors);
	}
}
