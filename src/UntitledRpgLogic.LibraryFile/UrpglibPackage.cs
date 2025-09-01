using System.Formats.Tar;
using System.IO.Compression;


namespace UntitledRpgLogic.LibraryFile;

/// <summary>
/// Represents the complete, deserialized .urpglib package,
/// providing access to its manifest and a stream for its payload data.
/// </summary>
public sealed class UrpglibPackage : IDisposable
{
	/// <summary>
	/// The structured header data read from the file.
	/// </summary>
	public UrpglibHeader Header { get; }

	/// <summary>
	/// The deserialized manifest containing package metadata.
	/// </summary>
	public PackageManifest? Manifest { get; }

	private readonly Stream payloadStream;
	private bool disposed;

	internal UrpglibPackage(UrpglibHeader header, PackageManifest? manifest, Stream payloadStream)
	{
		this.Header = header;
		this.Manifest = manifest;
		this.payloadStream = payloadStream;
	}

	/// <summary>
	/// Opens the compressed payload as a TarReader for iterating through the contained files.
	/// The caller is responsible for disposing the TarReader.
	/// </summary>
	/// <returns>A new TarReader instance for the payload.</returns>
	/// <exception cref="InvalidOperationException">Thrown if the package is disposed.</exception>
	public TarReader OpenPayload()
	{
		if (this.disposed)
		{
			throw new InvalidOperationException("Cannot open payload from a disposed package.");
		}

		// Reset position to the beginning of the payload stream for reading.
		this.payloadStream.Position = 0;

		var decompressionStream = this.Header.PayloadCompression switch
		{
			PayloadCompressionType.Gzip => new GZipStream(this.payloadStream, CompressionMode.Decompress, leaveOpen: true),
			PayloadCompressionType.None => this.payloadStream,
			_ => throw new NotSupportedException($"Payload compression type '{this.Header.PayloadCompression}' is not supported.")
		};

		return new TarReader(decompressionStream);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (this.disposed)
		{
			return;
		}

		this.payloadStream.Dispose();
		this.disposed = true;
	}
}

#region --- Usage Example ---
/*
public static class Example
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("--- Creating .urpglib package ---");

        // 1. Define the manifest for our new package
        var myManifest = new PackageManifest
        {
            Id = Guid.NewGuid(),
            Name = "Core Gameplay Data",
            Description = "Contains all essential items and monster definitions for the game.",
            Version = "1.0.1",
            VersionNumber = 101,
            AuthorName = "Nightwave Studios",
            AuthorId = Guid.NewGuid(),
            Dependencies = new[] { new Guid("A3E6D8B1-3E4C-4B1F-8A0A-1B2C3D4E5F6A") } // Example dependency
        };

        // 2. Define the files to be included in the payload
        var filesToPackage = new Dictionary<string, byte[]>
        {
            { "items/swords/longsword.toml", Encoding.UTF8.GetBytes("name = \"Longsword\"\ndamage = 10") },
            { "items/potions/health.toml", Encoding.UTF8.GetBytes("name = \"Health Potion\"\nrestores = 50") },
            { "monsters/goblin.toml", Encoding.UTF8.GetBytes("name = \"Goblin\"\nhealth = 30\nloot_table = \"common\"") }
        };

        string packagePath = "CoreData.urpglib";
        await UrpgWriter.WriteAsync(packagePath, myManifest, filesToPackage);
        Console.WriteLine($"Successfully created '{packagePath}'");

        Console.WriteLine("\n--- Reading .urpglib package ---");

        // 3. Read the package back
        try
        {
            await using var package = await UrpgReader.ReadAsync(packagePath);
            
            Console.WriteLine($"Package Name: {package.Manifest.Name}");
            Console.WriteLine($"Author: {package.Manifest.AuthorName}");
            Console.WriteLine($"Version: {package.Manifest.Version}");
            Console.WriteLine($"Payload Compression: {package.Header.PayloadCompression}");
            Console.WriteLine($"Dependencies: {string.Join(", ", package.Manifest.Dependencies)}");

            // 4. Iterate through the files in the payload
            Console.WriteLine("\nFiles in payload:");
            using var tarReader = package.OpenPayload();
            while (await tarReader.GetNextEntryAsync() is { } entry)
            {
                if (entry.DataStream != null)
                {
                    using var reader = new StreamReader(entry.DataStream, Encoding.UTF8);
                    string content = await reader.ReadToEndAsync();
                    Console.WriteLine($" - {entry.Name} (Size: {entry.Length} bytes)");
                    Console.WriteLine($"   Content: {content.Replace("\n", ", ")}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.ResetColor();
        }
    }
}
*/
#endregion
