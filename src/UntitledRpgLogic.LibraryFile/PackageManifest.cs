namespace UntitledRpgLogic.LibraryFile;

/// <summary>
/// Represents the package metadata, serialized to JSON in the manifest section.
/// </summary>
public sealed record PackageManifest
{
	/// <summary>
	/// The unique identifier for the library package.
	/// </summary>
	public Guid Id { get; set; } = Guid.NewGuid();
	/// <summary>
	/// The name of the library package.
	/// </summary>
	public required string Name { get; set; }
	/// <summary>
	/// The description of the library package, providing details about its purpose and contents.
	/// </summary>
	public string Description { get; set; } = string.Empty;

	private string version = "1.0.0";
	/// <summary>
	///		The version of the library package, following semantic versioning (major.minor.patch).
	/// </summary>
	public string Version
	{
		get => this.version;
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			this.version = value;
			this.ParseVersion(value);
		}
	}
	private void ParseVersion(string versionString)
	{
		var parts = versionString.Split('.');
		this.MajorVersion = parts.Length > 0 && int.TryParse(parts[0], out var major) ? major : 0;
		this.MinorVersion = parts.Length > 1 && int.TryParse(parts[1], out var minor) ? minor : 0;
		this.PatchVersion = parts.Length > 2 && int.TryParse(parts[2], out var patch) ? patch : 0;
	}

	/// <summary>
	///		The major version number of the library package, indicating significant changes or updates.
	/// </summary>
	public int MajorVersion { get; private set; }
	/// <summary>
	///		The minor version number of the library package, indicating smaller updates or improvements.
	/// </summary>
	public int MinorVersion { get; private set; }
	/// <summary>
	///		The patch version number of the library package, indicating bug fixes or minor changes.
	/// </summary>
	public int PatchVersion { get; private set; }


	/// <summary>
	///		The name of the author or organization that created the library package.
	/// </summary>
	public required string AuthorName { get; set; }
	/// <summary>
	///		The unique identifier of the author or organization that created the library package.
	/// </summary>
	public Guid AuthorId { get; set; } = Guid.NewGuid();
	/// <summary>
	///		A list of dependencies for this library package, represented by their unique identifiers. Includes the minimum required version for each dependency.
	/// </summary>
	public KeyValuePair<Guid, int>[] Dependencies { get; set; } = [];
}
