---
_layout: landing
---

# UntitledRpg.LibraryFile API Guide

The `UntitledRpg.LibraryFile` library provides a high-level API for reading, streaming, validating, and generating `.urpglib` container files. The container bundles metadata (`PackageManifest`) and compressed `.tar` payloads containing game definitions and configurations.

---

## Core Types

* **`UrpgFile`**: Static facade providing primary entry points for inspection, extraction, packaging, and validation.
* **`UrpglibPackage`**: An open package instance providing access to header metadata, manifest information, and payload contents.


* **`UrpgPackageEntry`**: A single file entry within the payload stream, offering zero-allocation stream access, UTF-8 string parsing, or raw byte loading.
* **`UrpgPackageBuilder`**: Fluent builder for creating new `.urpglib` archives from directories, files, or in-memory byte arrays.
* **`UrpgValidationResult`**: Diagnostic report containing status flags, schema versions, manifest data, and error details.

---

## Common Workflows

### 1. Fast Metadata Peeking

Read package metadata without decompressing or allocating the payload:

```csharp
using UntitledRpg.LibraryFile;

// Inspect metadata from a file path
PackageManifest manifest = await UrpgFile.ReadManifestAsync("CoreGameplay.urpglib");

Console.WriteLine($"Package: {manifest.Name} v{manifest.Version}");
Console.WriteLine($"Author: {manifest.AuthorName} ({manifest.AuthorId})");
Console.WriteLine($"Dependencies: {manifest.Dependencies.Count}");

// Inspect metadata directly from an open Stream
await using var networkStream = GetPackageStream();
PackageManifest remoteManifest = await UrpgFile.ReadManifestAsync(networkStream);

```

### 2. Streaming Payload Entries (Database Ingestion)

Stream entries sequentially to parse files directly into records without holding the entire archive in memory:

```csharp
using UntitledRpg.LibraryFile;

await using var package = await UrpgFile.OpenReadAsync("CoreGameplay.urpglib");

// Iterate over entries directly from the decompressed Tar stream
await foreach (UrpgPackageEntry entry in package.ReadEntriesAsync())
{
    if (entry.Name.EndsWith(".toml", StringComparison.OrdinalIgnoreCase))
    {
        // Read directly as UTF-8 text for TOML/JSON parsers
        string tomlText = await entry.ReadAsStringAsync();
        GameDefinition definition = TomlParser.Parse(tomlText);

        await database.UpsertDefinitionAsync(definition);
    }
    else if (entry.Name.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
    {
        // Or stream raw bytes
        byte[] rawBytes = await entry.ReadAsBytesAsync();
        await ProcessBinaryAssetAsync(entry.Name, rawBytes);
    }
}

```

### 3. Extracting an Archive to Disk

Unpack an archive back into its directory structure:

```csharp
using UntitledRpg.LibraryFile;

// One-liner extraction
await UrpgFile.ExtractToDirectoryAsync(
    packagePath: "CoreGameplay.urpglib", 
    destinationDirectory: "./GameData/Extracted", 
    overwrite: true);

// Or extract from an already opened package
await using var package = await UrpgFile.OpenReadAsync("CoreGameplay.urpglib");
await package.ExtractToDirectoryAsync("./GameData/Extracted", overwrite: true);

```

### 4. Validating Archive Integrity

Verify binary magic bytes, schema versions, manifest JSON deserialization, and payload decompression without throwing exceptions:

```csharp
using UntitledRpg.LibraryFile;

UrpgValidationResult result = await UrpgFile.ValidateAsync("ModPackage.urpglib", validatePayload: true);

if (!result.IsValid)
{
    Console.WriteLine("Package is invalid or corrupt:");
    foreach (string error in result.Errors)
    {
        Console.WriteLine($" - {error}");
    }
    return;
}

Console.WriteLine($"Valid package! Found {result.EntryCount} payload entries.");
Console.WriteLine($"Payload compression: {result.Header?.PayloadCompression}");
Console.WriteLine($"Manifest Schema: {result.Header?.ManifestSchemaVersion}");

```

### 5. Packaging and Creating Archives

Create archives using either the one-liner helper or the fluent builder.

#### Quick Directory Packaging:

```csharp
using UntitledRpg.LibraryFile;

var manifest = new PackageManifest
{
    Name = "Base Items Mod",
    AuthorName = "Dev Studio",
    Version = new Version(1, 0, 0),
    Description = "Base equipment and item configurations"
};

await UrpgFile.CreateFromDirectoryAsync(
    sourceDirectory: "./RawContent/Items",
    destinationPackagePath: "./Output/Items.urpglib",
    manifest: manifest,
    compression: PayloadCompressionType.Gzip);

```

#### Advanced Package Assembly:

```csharp
using System.Text;
using UntitledRpg.LibraryFile;

var manifest = new PackageManifest
{
    Name = "Custom Encounter",
    AuthorName = "Scenario Designer",
    Version = new Version(2, 1, 0)
};

byte[] generatedConfig = Encoding.UTF8.GetBytes("difficulty = 5\nenemy_count = 12");

await new UrpgPackageBuilder()
    .WithManifest(manifest)
    .WithCompression(PayloadCompressionType.Gzip)
    .AddDirectory("./Content/Monsters", entryPrefix: "monsters")
    .AddFile("./Shared/LootTables.toml", entryName: "tables/loot.toml")
    .AddBytes(generatedConfig, entryName: "rules/generated_encounter.toml")
    .BuildAsync("./Output/Encounter.urpglib");

```

### 6. In-Memory Random Access

For modding tools or editors that need random access by file path rather than batch-streaming through all entries:

```csharp
using UntitledRpg.LibraryFile;

await using var package = await UrpgFile.OpenReadAsync("CoreGameplay.urpglib");

// Unpacks all files into memory at once
IReadOnlyDictionary<string, byte[]> fileMap = await package.ReadAllToMemoryAsync();

if (fileMap.TryGetValue("items/weapons/sword.toml", out byte[]? swordData))
{
    string swordToml = Encoding.UTF8.GetString(swordData);
    Console.WriteLine(swordToml);
}

```

---

## Best Practices

* **Use `ReadEntriesAsync()` for Batch Processing:** Streaming avoids buffering entire archives into memory when pushing records into databases or search indexes.
* **Use `ReadManifestAsync()` for Fast Scans:** Use this in mod loaders or main menus to build metadata catalogs without paying decompression overhead.


* **Handle Disposal Cleanly:** `UrpglibPackage` implements both `IDisposable` and `IAsyncDisposable`. When using asynchronous flows, prefer `await using var package = await UrpgFile.OpenReadAsync(...)` to ensure internal streams and file locks release cleanly.age.
