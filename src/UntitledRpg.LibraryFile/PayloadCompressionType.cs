namespace UntitledRpgLogic.LibraryFile;

/// <summary>
/// Defines the supported payload compression algorithms.
/// </summary>
public enum PayloadCompressionType
{
	/// <summary>
	///		No compression (e.g. appended files only; like a .tar file).
	/// </summary>
	None = 0x00,
	/// <summary>
	///		Gzip compression (RFC 1952)
	/// </summary>
	Gzip = 0x01,
}
