using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UntitledRpg.LibraryFile;

/// <inheritdoc />
public sealed class VersionJsonConverter : JsonConverter<Version>
{
	/// <inheritdoc />
	public override Version? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return null;
		}

		if (reader.TokenType != JsonTokenType.String)
		{
			throw new JsonException($"Expected string when deserializing Version, but received {reader.TokenType}.");
		}

		var raw = reader.GetString();
		return raw is null ? null : new Version(raw);
	}

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(writer);

		if (value is null)
		{
			writer.WriteNullValue();
			return;
		}

		writer.WriteStringValue(value.ToString());
	}
}
