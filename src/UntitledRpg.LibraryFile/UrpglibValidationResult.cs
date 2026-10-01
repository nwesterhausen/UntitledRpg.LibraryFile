using System.Collections.Generic;

namespace UntitledRpg.LibraryFile;

/// <summary>
///     Represents the result of an archive validation check.
/// </summary>
public sealed record UrpglibValidationResult
{
	/// <summary>
	///     True if the file adheres to the specification and has zero validation errors.
	/// </summary>
	public bool IsValid => this.Errors.Count == 0;

	/// <summary>
	///     List of validation error descriptions, if any were found.
	/// </summary>
	public IReadOnlyList<string> Errors { get; init; } = [];

	/// <summary>
	///     The parsed header information, or null if the header was invalid.
	/// </summary>
	public UrpglibHeader? Header { get; init; }

	/// <summary>
	///     The deserialized package manifest, or null if missing or corrupt.
	/// </summary>
	public PackageManifest? Manifest { get; init; }

	/// <summary>
	///     Total number of files identified in the payload (if payload validation was requested).
	/// </summary>
	public int EntryCount { get; init; }
}
