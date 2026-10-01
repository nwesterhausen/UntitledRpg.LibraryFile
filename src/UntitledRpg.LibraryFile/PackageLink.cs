using System;

namespace UntitledRpg.LibraryFile;

/// <summary>
///     Describes a link a package may have to another package.
/// </summary>
/// <param name="PackageId">The unique id of the linked package</param>
/// <param name="Version">The required version of the linked package</param>
/// <param name="AllowMinorUpdates">Whether anything in the major version is allowed, or (if false) version must be exact</param>
public record PackageLink(Ulid PackageId, string Version, bool AllowMinorUpdates = true);
