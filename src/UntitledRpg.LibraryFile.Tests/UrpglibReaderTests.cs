namespace UntitledRpg.LibraryFile.Tests;

[TestClass]
public sealed class UrpglibReaderTests : IDisposable
{
	private readonly string tempDirectory;

	public UrpglibReaderTests()
	{
		this.tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		_ = Directory.CreateDirectory(this.tempDirectory);
	}

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
		var manifest = HelperMethods.CreateTestManifest();
		await HelperMethods.CreateValidUrpglibFile(filePath, manifest).ConfigureAwait(true);

		// Act
		var package = await UrpglibReader.ReadAsync(filePath, true).ConfigureAwait(true);

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
		var manifest = HelperMethods.CreateTestManifest();
		await HelperMethods.CreateValidUrpglibFile(filePath, manifest).ConfigureAwait(false);

		// Act
		var package = await UrpglibReader.ReadAsync(filePath).ConfigureAwait(false);

		// Assert
		using (package)
		{
			Assert.IsNotNull(package);
			Assert.IsNotNull(package.Header);
			Assert.IsNotNull(package.Manifest);
			Assert.AreEqual(manifest.Name, package.Manifest.Name);
			Assert.AreEqual(manifest.AuthorName, package.Manifest.AuthorName);
			Assert.AreEqual(manifest.Id, package.Manifest.Id, "Manifest Id did not survive JSON round-trip.");

			if (manifest.Dependencies != null)
			{
				Assert.IsNotNull(package.Manifest.Dependencies);
				Assert.HasCount(manifest.Dependencies.Count, package.Manifest.Dependencies);

				for (var i = 0; i < manifest.Dependencies.Count; i++)
				{
					Assert.AreEqual(manifest.Dependencies[i].PackageId, package.Manifest.Dependencies[i].PackageId);
					Assert.AreEqual(manifest.Dependencies[i].Version, package.Manifest.Dependencies[i].Version);
				}
			}
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
		var exception = await Assert
			.ThrowsExactlyAsync<UrpglibInvalidSignatureException>(() => UrpglibReader.ReadAsync(filePath))
			.ConfigureAwait(false);

		Assert.IsNotNull(exception);
		Assert.IsTrue(exception.Message.Contains("Invalid file signature",
			StringComparison.InvariantCultureIgnoreCase));
	}

	[TestMethod]
	public async Task ReadAsync_FileWithNewerHeaderVersion_ThrowsUrpglibVersionMismatchException()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "newer_version.urpglib");
		var manifest = HelperMethods.CreateTestManifest();
		await HelperMethods
			.CreateUrpglibFileWithVersion(filePath, manifest, UrpglibConstants.CurrentHeaderSchemaVersion + 1)
			.ConfigureAwait(false);

		// Act & Assert
		var exception = await Assert
			.ThrowsExactlyAsync<UrpglibVersionMismatchException>(() => UrpglibReader.ReadAsync(filePath))
			.ConfigureAwait(false);

		Assert.IsNotNull(exception);
		Assert.IsTrue(exception.Message.Contains("newer than supported version",
			StringComparison.InvariantCultureIgnoreCase));
	}

	[TestMethod]
	public async Task ReadAsync_FileWithInvalidManifest_ThrowsUrpglibFileFormatException()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "invalid_manifest.urpglib");
		await HelperMethods.CreateUrpglibFileWithInvalidManifest(filePath).ConfigureAwait(false);

		// Act & Assert
		var exception = await Assert
			.ThrowsExactlyAsync<UrpglibManifestDeserializationException>(() => UrpglibReader.ReadAsync(filePath))
			.ConfigureAwait(false);

		Assert.IsNotNull(exception);
		Assert.IsTrue(exception.Message.Contains("Failed to deserialize package manifest",
			StringComparison.InvariantCultureIgnoreCase));
	}

	[TestMethod]
	public async Task ReadAsync_EmptyFile_ThrowsException()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "empty.urpglib");
		await File.WriteAllBytesAsync(filePath, []).ConfigureAwait(false);

		// Act & Assert
		_ = await Assert.ThrowsExactlyAsync<UrpglibFileSizeException>(() => UrpglibReader.ReadAsync(filePath))
			.ConfigureAwait(false);
	}

	[TestMethod]
	public async Task ReadAsync_FileWithGzipCompression_ReadsCorrectly()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "gzip_test.urpglib");
		var manifest = HelperMethods.CreateTestManifest();
		await HelperMethods.CreateValidUrpglibFile(filePath, manifest, PayloadCompressionType.Gzip)
			.ConfigureAwait(false);

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
		var manifest = HelperMethods.CreateTestManifest();
		await HelperMethods.CreateValidUrpglibFile(filePath, manifest).ConfigureAwait(false);

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
		var manifest = HelperMethods.CreateTestManifest();
		manifest = manifest with
		{
			Description = new string('A', 10000) // Large description
		};
		await HelperMethods.CreateValidUrpglibFile(filePath, manifest).ConfigureAwait(false);

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
		_ = await Assert.ThrowsExactlyAsync<FileNotFoundException>(() => UrpglibReader.ReadAsync(filePath))
			.ConfigureAwait(false);
	}

	[TestMethod]
	public async Task ReadAsync_TruncatedFile_ThrowsEndOfStreamException()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "truncated.urpglib");
		// The file actually doesn't contain the full manifest data, simulating a truncated file
		await HelperMethods.CreateUrpglibFileWithManifestLength(filePath, 10).ConfigureAwait(false);

		// Act & Assert
		_ = await Assert.ThrowsExactlyAsync<UrpglibFileFormatException>(() => UrpglibReader.ReadAsync(filePath))
			.ConfigureAwait(false);
	}

	[TestMethod]
	public async Task ReadAsync_FileWithZeroManifestLength_ThrowsFileFormatException()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "zero_manifest.urpglib");
		await HelperMethods.CreateUrpglibFileWithManifestLength(filePath, 0).ConfigureAwait(false);

		// Act & Assert
		_ = await Assert.ThrowsExactlyAsync<UrpglibFileFormatException>(() => UrpglibReader.ReadAsync(filePath))
			.ConfigureAwait(false);
	}

	[TestMethod]
	public async Task ReadAsync_MultipleConcurrentReads_AllSucceed()
	{
		// Arrange
		var filePath = Path.Combine(this.tempDirectory, "concurrent_test.urpglib");
		var manifest = HelperMethods.CreateTestManifest();
		await HelperMethods.CreateValidUrpglibFile(filePath, manifest).ConfigureAwait(false);

		// Act
		var tasks = Enumerable.Range(0, 10).Select(async _ =>
		{
			var package = await UrpglibReader.ReadAsync(filePath, true).ConfigureAwait(false);
			using (package)
			{
				Assert.IsNotNull(package.Manifest);
				return package.Manifest.Name;
			}
		});

		var results = await Task.WhenAll(tasks).ConfigureAwait(false);

		// Assert
		Assert.HasCount(10, results);
		foreach (var result in results)
		{
			Assert.AreEqual(manifest.Name, result);
		}
	}

	[TestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public async Task ReadAsync_CanOpenPayloadAndReadEntries(bool readIntoMemory)
	{
		var filePath = Path.Combine(this.tempDirectory, $"payload_test_{readIntoMemory}.urpglib");
		var manifest = HelperMethods.CreateTestManifest();
		var files = new Dictionary<string, byte[]>
		{
			["file1.txt"] = "Hello World"u8.ToArray(),
			["file2.bin"] = new byte[] { 0x01, 0x02, 0x03, 0x04 }
		};

		await UrpglibWriter.WriteAsync(filePath, manifest, files).ConfigureAwait(false);

		var package = await UrpglibReader.ReadAsync(filePath, readIntoMemory).ConfigureAwait(false);
		await using (package.ConfigureAwait(false))
		{
			// 1. Verify direct TarReader access
			var tarReader = package.OpenPayload();
			await using (tarReader.ConfigureAwait(false))
			{
				var entry = await tarReader.GetNextEntryAsync().ConfigureAwait(false);
				Assert.IsNotNull(entry);
				Assert.AreEqual("file1.txt", entry.Name);
			}

			// 2. Verify single-pass ReadEntriesAsync streaming
			var entriesRead = 0;
			await foreach (var pkgEntry in package.ReadEntriesAsync().ConfigureAwait(false))
			{
				entriesRead++;
				if (pkgEntry.Name == "file1.txt")
				{
					var content = await pkgEntry.ReadAsStringAsync().ConfigureAwait(false);
					Assert.AreEqual("Hello World", content);
				}
				else if (pkgEntry.Name == "file2.bin")
				{
					var bytes = await pkgEntry.ReadAsBytesAsync().ConfigureAwait(false);
					CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04 }, bytes);
				}
			}

			Assert.AreEqual(2, entriesRead);
		}
	}
}
