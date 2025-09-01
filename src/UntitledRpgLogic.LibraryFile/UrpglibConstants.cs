using System.Text.Json;

namespace UntitledRpgLogic.LibraryFile;

/// <summary>
///		Constants used throughout the URPG library file format library.
/// </summary>
public static class UrpglibConstants
{
	/// <summary>
	/// The unique 8-byte signature for .urpglib files.
	/// Hex: BA 4E 57 7E 52 50 47 1A
	/// ASCII: ºNW~RPG.
	/// </summary>
	public static readonly byte[] MagicBytes = [0xBA, 0x4E, 0x57, 0x7E, 0x52, 0x50, 0x47, 0x1A];

	/// <summary>
	/// The current version of the header structure this library can write.
	/// </summary>
	public const byte CurrentHeaderSchemaVersion = 1;

	/// <summary>
	/// The current version of the manifest structure this library can write.
	/// </summary>
	public const ushort CurrentManifestSchemaVersion = 1;

	/// <summary>
	/// The current version of the payload data structure this library can write.
	/// </summary>
	public const ushort CurrentPayloadSchemaVersion = 1;

	/// <summary>
	///		The minimum size of a valid .urpglib file in bytes.
	/// </summary>
	/// <remarks>
	/// Based on the file format specification, the header section is 18 bytes total.
	/// <list type="bullet">
	///   <item>
	///     <description>Magic Bytes: 8 bytes</description>
	///   </item>
	///   <item>
	///     <description>Header Schema Version: 1 byte</description>
	///   </item>
	///   <item>
	///     <description>Manifest Schema Version: 2 bytes</description>
	///   </item>
	///   <item>
	///     <description>Payload Compression: 1 byte</description>
	///   </item>
	///   <item>
	///     <description>Payload Schema Version: 2 bytes</description>
	///   </item>
	///   <item>
	///     <description>Manifest Length: 4 bytes</description>
	///   </item>
	/// </list>
	/// Total: 8 + 1 + 2 + 1 + 2 + 4 = 18 bytes
	/// </remarks>
	public const long MinFileSize = 18;

	/// <summary>
	/// Gets the default <see cref="JsonSerializerOptions"/> instance configured with the expected urpglib file settings.
	/// </summary>
	/// <remarks>The default options use camel case for property naming and do not write indented JSON. These
	/// settings are commonly used for consistent and compact JSON serialization.</remarks>
	public static readonly JsonSerializerOptions? DefaultJsonSerializerOptions = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false
	};
}
