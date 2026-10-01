using System;

namespace UntitledRpg.LibraryFile;

/// <summary>
///     Describes the version of a package. Uses semantic versioning.
/// </summary>
public sealed record Version : IComparable, IComparable<Version?>
{
	/// <summary>
	///     Create a version from a version string
	/// </summary>
	/// <param name="versionString"></param>
	/// <exception cref="ArgumentNullException">If <paramref name="versionString" /> is null</exception>
	public Version(string versionString)
	{
		this.RawVersion = versionString;

		ArgumentNullException.ThrowIfNull(versionString);
		this.ParseVersion(versionString);
	}

	/// <summary>
	///     Create a Version specifying the parts
	/// </summary>
	/// <param name="major"></param>
	/// <param name="minor"></param>
	/// <param name="patch"></param>
	public Version(int major = 1, int minor = 0, int patch = 0)
	{
		this.MajorVersion = major;
		this.MinorVersion = minor;
		this.PatchVersion = patch;
		this.RawVersion = $"{major}.{minor}.{patch}";
		this.ParseVersion(this.RawVersion);
	}

	/// <summary>
	///     Create a new version at version 1.0.0
	/// </summary>
	public Version() : this(1)
	{
	}

	/// <summary>
	///     The major version number of the library package, indicating significant changes or updates.
	/// </summary>
	public int MajorVersion { get; private set; }

	/// <summary>
	///     The minor version number of the library package, indicating smaller updates or improvements.
	/// </summary>
	public int MinorVersion { get; private set; }

	/// <summary>
	///     The patch version number of the library package, indicating bug fixes or minor changes.
	/// </summary>
	public int PatchVersion { get; private set; }

	/// <summary>
	///     The string version assigned to this Version at creation.
	/// </summary>
	public string RawVersion { get; } = "1.0.0";

	/// <summary>
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	public int CompareTo(object? obj)
	{
		if (obj == null)
		{
			return 1;
		}

		if (obj is Version other)
		{
			return this.CompareTo(other);
		}

		throw new ArgumentException($"Object must be of type {nameof(Version)}");
	}

	/// <inheritdoc />
	public int CompareTo(Version? other)
	{
		if (other == null)
		{
			return 1;
		}

		var cmp = other.MajorVersion - this.MajorVersion;
		if (cmp != 0)
		{
			return cmp;
		}

		cmp = other.MinorVersion - this.MinorVersion;
		if (cmp != 0)
		{
			return cmp;
		}

		return other.PatchVersion - this.PatchVersion;
	}

	private void ParseVersion(string versionString)
	{
		var parts = versionString.Split('.');
		this.MajorVersion = parts.Length > 0 && int.TryParse(parts[0], out var major) ? major : 0;
		this.MinorVersion = parts.Length > 1 && int.TryParse(parts[1], out var minor) ? minor : 0;
		this.PatchVersion = parts.Length > 2 && int.TryParse(parts[2], out var patch) ? patch : 0;
	}

	/// <summary>
	///		Compare one version to another.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static int Compare(Version? left, Version? right)
	{
		if (ReferenceEquals(left, right))
		{
			return 0;
		}

		if (left is null)
		{
			return -1;
		}

		return left.CompareTo(right);
	}

	/// <inheritdoc />
	public override string ToString() => $"{this.MajorVersion}.{this.MinorVersion}.{this.PatchVersion}";
}
