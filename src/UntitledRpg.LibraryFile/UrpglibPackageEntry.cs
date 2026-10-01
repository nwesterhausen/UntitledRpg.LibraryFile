using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UntitledRpg.LibraryFile;

/// <summary>
///     Represents an individual file entry contained within a .urpglib payload.
/// </summary>
public sealed class UrpglibPackageEntry
{
	private readonly Stream dataStream;

	internal UrpglibPackageEntry(string name, long length, Stream dataStream)
	{
		this.Name = name;
		this.Length = length;
		this.dataStream = dataStream;
	}

	/// <summary>
	///     The relative file path inside the package (e.g., "monsters/goblin.toml").
	/// </summary>
	public string Name { get; }

	/// <summary>
	///     The uncompressed size of the entry in bytes.
	/// </summary>
	public long Length { get; }

	/// <summary>
	///     Reads the entry stream content as a UTF-8 encoded string.
	/// </summary>
	public async Task<string> ReadAsStringAsync(CancellationToken ct = default)
	{
		using var reader = new StreamReader(
			this.dataStream,
			Encoding.UTF8,
			true,
			4096,
			true);

		return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
	}

	/// <summary>
	///     Reads the entry stream content directly into a byte array.
	/// </summary>
	public async Task<byte[]> ReadAsBytesAsync(CancellationToken ct = default)
	{
		using var ms = new MemoryStream(this.Length > 0 ? (int)this.Length : 0);
		await this.dataStream.CopyToAsync(ms, ct).ConfigureAwait(false);
		return ms.ToArray();
	}

	/// <summary>
	///     Exposes the forward-only data stream for custom parsers.
	/// </summary>
	public Stream OpenStream() => this.dataStream;
}
