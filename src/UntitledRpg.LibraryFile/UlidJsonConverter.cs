using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UntitledRpgLogic.LibraryFile;

/// <summary>
///     Enables System.Text.Json to serialize and deserialize <see cref="Ulid" /> values to and from Base32 strings.
///     (This should be the default behavior, but explicitly defining a converter like this ensures it.)
/// </summary>
public sealed class UlidJsonConverter : JsonConverter<Ulid>
{
	/// <inheritdoc />
	public override Ulid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var value = reader.GetString();
		return string.IsNullOrEmpty(value) ? Ulid.Empty : Ulid.Parse(value, CultureInfo.InvariantCulture);
	}

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, Ulid value, JsonSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.WriteStringValue(value.ToString());
	}

	/// <inheritdoc />
	public override Ulid ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert,
		JsonSerializerOptions options)
	{
		var value = reader.GetString();
		return string.IsNullOrEmpty(value) ? Ulid.Empty : Ulid.Parse(value, CultureInfo.InvariantCulture);
	}

	/// <inheritdoc />
	public override void WriteAsPropertyName(Utf8JsonWriter writer, Ulid value, JsonSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.WritePropertyName(value.ToString());
	}
}
