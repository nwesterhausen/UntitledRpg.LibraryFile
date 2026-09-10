using System;
using System.Diagnostics.CodeAnalysis;

namespace UntitledRpgLogic.LibraryFile;
#pragma warning disable CA1303
/// <summary>
/// 	Represents an exception that is thrown when a file does not conform to the expected URPG file format.
/// </summary>
/// <remarks>
/// 	This exception is typically used to indicate that a file being processed is invalid or corrupted according
/// 	to the URPG file format specification. Ensure that the file being provided adheres to the expected format
/// 	before attempting to process it.
/// </remarks>
public class UrpglibFileFormatException : Exception
{
	/// <summary>
	/// 	Represents an exception that is thrown when an error occurs related to the URPG file format.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	public UrpglibFileFormatException(string message) : base(message) { }

	/// <summary>
	/// 	Represents an exception that is thrown when an error occurs while processing a file in the Urpglib format.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="innerException">The exception that caused the current exception, or <see langword="null"/> if no inner exception is specified.</param>
	public UrpglibFileFormatException(string message, Exception innerException) : base(message, innerException) { }

	/// <summary>
	/// 	Throw a <see cref="UrpglibFileFormatException" /> with speicfied message.
	/// </summary>
	/// <param name="message">Details for the exception</param>
	[DoesNotReturn]
	public static void Throw(string message) =>
		throw new UrpglibFileFormatException(message);

	/// <summary>
	/// 	Throw a <see cref="UrpglibFileFormatException" /> with speicfied message if a condition is met.
	/// </summary>
	/// <param name="condition">Condition that must be true to thow</param>
	/// <param name="message">Details for the exception</param>
	public static void ThrowIf(bool condition, string message)
	{
		if (condition)
		{
			Throw(message);
		}
	}
}

/// <summary>
/// 	Represents an exception that is thrown when a version mismatch is detected in a URPG file.
/// </summary>
/// <remarks>
/// 	This exception is typically thrown when the version of a URPG file being processed does not match the
/// 	expected version. It indicates that the file format may be incompatible with the current implementation.
/// </remarks>
public class UrpglibVersionMismatchException : UrpglibFileFormatException
{
	/// <summary>
	/// 	Represents an exception that is thrown when there is a version mismatch in the URPG schema versions between the file and the library.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	public UrpglibVersionMismatchException(string message) : base(message) { }

	/// <summary>
	/// 	Represents an exception that is thrown when there is a version mismatch in the URPGLib library.
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	/// <param name="innerException">The exception that caused the current exception, or <see langword="null"/> if no inner exception is specified.</param>
	public UrpglibVersionMismatchException(string message, Exception innerException) : base(message, innerException) { }

	/// <summary>
	/// 	Throw a <see cref="UrpglibVersionMismatchException" /> with speicfied message.
	/// </summary>
	/// <param name="message">Details for the exception</param>
	[DoesNotReturn]
	public static new void Throw(string message) =>
		throw new UrpglibVersionMismatchException(message);

	/// <summary>
	/// 	Throw a <see cref="UrpglibVersionMismatchException" /> with speicfied message if a condition is met.
	/// </summary>
	/// <param name="condition">Condition that must be true to thow</param>
	/// <param name="message">Details for the exception</param>
	public static new void ThrowIf(bool condition, string message)
	{
		if (condition)
		{
			Throw(message);
		}
	}
}

/// <summary>
/// 	The exception that is thrown when a URPG file is smaller than the minimum required size.
/// </summary>
/// <remarks>
/// 	This exception is typically thrown during the validation of URPG file formats when the file size does
/// 	not meet the minimum requirements. It indicates that the file is incomplete or corrupted.
/// </remarks>
public class UrpglibFileSizeException : UrpglibFileFormatException
{
	/// <summary>
	/// 	Represents an exception that is thrown when a URPG file is smaller than the minimum required size.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	public UrpglibFileSizeException(string message) : base(message) { }
	/// <summary>
	/// 	Represents an exception that is thrown when a URPG file is smaller than the minimum required size.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="innerException">The exception that caused the current exception, or <see langword="null"/> if no inner exception is specified.</param>
	public UrpglibFileSizeException(string message, Exception innerException) : base(message, innerException) { }

	/// <summary>
	/// 	Throw a <see cref="UrpglibFileSizeException" /> with speicfied message.
	/// </summary>
	/// <param name="message">Details for the exception</param>
	[DoesNotReturn]
	public static new void Throw(string message) =>
		throw new UrpglibFileSizeException(message);

	/// <summary>
	/// 	Throw a <see cref="UrpglibFileSizeException" /> with speicfied message if a condition is met.
	/// </summary>
	/// <param name="condition">Condition that must be true to thow</param>
	/// <param name="message">Details for the exception</param>
	public static new void ThrowIf(bool condition, string message)
	{
		if (condition)
		{
			Throw(message);
		}
	}
}

/// <summary>
/// 	Exception for an invalid file signature.
/// </summary>
public sealed class UrpglibInvalidSignatureException : UrpglibFileFormatException
{
	/// <summary>
	/// 	Exception for an invalid file signature.
	/// </summary>
	public UrpglibInvalidSignatureException()
	: base("Invalid file signature. This is not a valid .urpglib file.") { }

	/// <summary>
	/// 	Helper to adhear to .Throw convention
	/// </summary>
	[DoesNotReturn]
	public static void Throw() =>
		throw new UrpglibInvalidSignatureException();
}

/// <summary>
/// 	Exception for a deserialization failure.
/// </summary>
public sealed class UrpglibManifestDeserializationException : UrpglibFileFormatException
{
	/// <summary>
	/// 	Exception for a deserialization failure.
	/// </summary>
	/// <param name="inner">The deserialization exception</param>
	public UrpglibManifestDeserializationException(Exception inner)
	: base("Failed to deserialize package manifest.", inner) { }

	/// <summary>
	/// 	Exception for a deserialization failure.
	/// </summary>
	public UrpglibManifestDeserializationException()
	: base("Failed to deserialize package manifest.") { }

	/// <summary>
	/// 	Helper to adhear to .Throw convention
	/// </summary>
	[DoesNotReturn]
	public static void Throw() =>
		throw new UrpglibManifestDeserializationException();

	/// <summary>
	/// 	Helper to adhear to .Throw convention
	/// </summary>
	/// <param name="inner">Inner exception</param>
	[DoesNotReturn]
	public static void ThrowWith(Exception inner) =>
			throw new UrpglibManifestDeserializationException(inner);
}

/// <summary>
/// 	Exception for an invalid payload size.
/// </summary>
public sealed class UrpglibInvalidPayloadSizeException : UrpglibFileFormatException
{
	/// <summary>
	/// 	Exception for an invalid payload size.
	/// </summary>
	public UrpglibInvalidPayloadSizeException()
	: base("Payload size is zero or negative. The file may be corrupted.") { }

	/// <summary>
	/// 	Helper to adhear to .Throw convention
	/// </summary>
	[DoesNotReturn]
	public static void Throw() =>
		throw new UrpglibInvalidPayloadSizeException();
}
#pragma warning restore CA1303
