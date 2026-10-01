using System.Text.Json;

namespace UntitledRpg.LibraryFile.Tests;

[TestClass]
public class VersionTests
{
	private readonly JsonSerializerOptions _options = new() { Converters = { new VersionJsonConverter() } };

	[TestMethod]
	public void Serialize_StandaloneVersion_WritesStringToken()
	{
		var version = new Version(2, 5, 1);

		var json = JsonSerializer.Serialize(version, this._options);

		Assert.AreEqual("\"2.5.1\"", json);
	}

	[TestMethod]
	[DataRow("\"1.0.0\"", 1, 0, 0)]
	[DataRow("\"0.14.2\"", 0, 14, 2)]
	[DataRow("\"3.2.1\"", 3, 2, 1)]
	public void Deserialize_ValidVersionString_ProducesExpectedVersion(
		string json,
		int expectedMajor,
		int expectedMinor,
		int expectedPatch)
	{
		var version = JsonSerializer.Deserialize<Version>(json, this._options);

		Assert.IsNotNull(version);
		Assert.AreEqual(expectedMajor, version.MajorVersion);
		Assert.AreEqual(expectedMinor, version.MinorVersion);
		Assert.AreEqual(expectedPatch, version.PatchVersion);
	}

	[TestMethod]
	public void SerializeAndDeserialize_WithinComplexType_RoundTripsCorrectly()
	{
		var manifest = HelperMethods.CreateTestManifest() with
		{
			Name = "AdventureModule",
			Version = new Version(1, 4)
		};

		var json = JsonSerializer.Serialize(manifest, this._options);
		var result = JsonSerializer.Deserialize<PackageManifest>(json, this._options);

		StringAssert.Contains(json, "\"Version\":\"1.4.0\"", StringComparison.Ordinal);
		Assert.IsNotNull(result);
		Assert.AreEqual("AdventureModule", result.Name);
		Assert.AreEqual(new Version(1, 4), result.Version);
	}

	[TestMethod]
	public void Serialize_NullVersion_WritesJsonNull()
	{
		Version? version = null;

		var json = JsonSerializer.Serialize(version, this._options);

		Assert.AreEqual("null", json);
	}

	[TestMethod]
	public void Deserialize_JsonNull_ReturnsNull()
	{
		var json = "null";

		var result = JsonSerializer.Deserialize<Version>(json, this._options);

		Assert.IsNull(result);
	}

	[TestMethod]
	public void Deserialize_InvalidTokenType_ThrowsJsonException()
	{
		// Supplying a JSON object instead of a string token
		var json = "{\"MajorVersion\": 1, \"MinorVersion\": 0, \"PatchVersion\": 0}";

		Assert.Throws<JsonException>(() =>
		{
			JsonSerializer.Deserialize<Version>(json, this._options);
		});
	}
}
