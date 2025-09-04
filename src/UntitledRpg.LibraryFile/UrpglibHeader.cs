namespace UntitledRpgLogic.LibraryFile;

/// <summary>
/// Contains the structured, binary data from the file header.
/// </summary>
public sealed record UrpglibHeader
{
	/// <summary>
	///		The version of the header schema used in this file.
	/// </summary>
	public byte HeaderSchemaVersion { get; init; }
	/// <summary>
	///		The version of the manifest schema used in this file.
	/// </summary>
	public ushort ManifestSchemaVersion { get; init; }
	/// <summary>
	///		The type of compression used for the payload data.
	/// </summary>
	public PayloadCompressionType PayloadCompression { get; init; }
	/// <summary>
	///		The version of the payload schema used in this file.
	/// </summary>
	public ushort PayloadSchemaVersion { get; init; }
	/// <summary>
	///		The length of the manifest section in bytes, which is serialized to JSON in the file.
	/// </summary>
	public uint ManifestLength { get; init; }
}
