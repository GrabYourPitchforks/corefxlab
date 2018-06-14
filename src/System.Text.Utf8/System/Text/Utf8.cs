// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;

using Char8 = System.Text.Utf8Char;

namespace System.Text
{
    // Contains helper methods for performing UTF-8 data conversion.
    // Alternative naming: class Utf8Convert in namespace System.Text.Utf8
    public static partial class Utf8_
    {
        // Converts UTF8 -> UTF16.
        // Similar to Utf8Encoder.Convert, but uses OperationStatus-style APIs.
        public static OperationStatus ConvertToUtf16(
            ReadOnlySpan<Char8> input,
            Span<char> output,
            out int elementsConsumed,
            out int elementsWritten,
            InvalidSequenceBehavior replacementBehavior = InvalidSequenceBehavior.ReplaceInvalidSequence,
            bool isFinalChunk = true) => throw null;

        // Converts UTF16 -> UTF8.
        // Similar to Utf8Encoder.Convert, but uses OperationStatus-style APIs.
        public static OperationStatus ConvertFromUtf16(
            ReadOnlySpan<char> input,
            Span<Char8> output,
            out int elementsConsumed,
            out int elementsWritten,
            InvalidSequenceBehavior replacementBehavior = InvalidSequenceBehavior.ReplaceInvalidSequence,
            bool isFinalChunk = true) => throw null;

        // Converts UTF8 -> UTF8, fixing up invalid sequences along the way.
        // (This will never return InvalidData.)
        public static OperationStatus ConvertToValidUtf8(
            ReadOnlySpan<Char8> input,
            Span<Char8> output,
            out int elementsConsumed,
            out int elementsWritten,
            bool isFinalChunk = true) => throw null;

        // Same as above, but input buffer has T=byte
        public static OperationStatus ConvertToValidUtf8(
            ReadOnlySpan<byte> input,
            Span<Char8> output,
            out int elementsConsumed,
            out int elementsWritten,
            bool isFinalChunk = true) => throw null;

        // If the input is valid UTF-8, returns the input as-is.
        // If the input is not valid UTF-8, replaces invalid sequences and returns
        // a Utf8String which is well-formed UTF-8.
        public static Utf8String ConvertToValidUtf8(Utf8String input) => throw null;

        // ROM<T>-based overload of above. Will return original input or allocate new buffer.
        public static ReadOnlyMemory<Char8> ConvertToValidUtf8(ReadOnlyMemory<Char8> input) => throw null;
    }

    public enum InvalidSequenceBehavior
    {
        ReplaceInvalidSequence, // replace invalid sequences with U+FFFD
        Fail, // don't repalce invalid sequences; report failure instead
    }

    // Contains helper methods for inspecting UTF-8 data.
    // Alternative naming: class Utf8Inspection in namespace System.Text.Utf8
    public static partial class Utf8_
    {
        // Allows forward enumeration over scalars.
        // TODO: Should we also allow reverse enumeration? GetScalarsReverse?
        public static Utf8SpanUnicodeScalarEnumerator GetScalars(ReadOnlySpan<byte> utf8Text) => throw null;
        public static Utf8SpanUnicodeScalarEnumerator GetScalars(ReadOnlySpan<Char8> utf8Text) => throw null;

        // Returns true iff the input consists only of well-formed UTF-8 sequences.
        // This is O(n).
        public static bool IsWellFormedUtf8Sequence(ReadOnlySpan<byte> utf8Text) => throw null;
        public static bool IsWellFormedUtf8Sequence(ReadOnlySpan<Char8> utf8Text) => throw null;

        // Returns true iff the Utf8String consists only of well-formed UTF-8 sequences.
        // This will almost always be O(1) but may be O(n) in weird edge cases.
        public static bool IsWellFormedUtf8String(Utf8String utf8String) => throw null;

        // Returns the first scalar in the sequence along with the sequence length consumed.
        // SequenceValidity's members have different semantic meaning from OperationStatus.
        // TODO: Should we have UTF-16 equivalents of these? Could be useful.
        public static SequenceValidity PeekFirstScalar(
            ReadOnlySpan<byte> utf8Text,
            out UnicodeScalar scalar,
            out int sequenceLength) => throw null;
        public static SequenceValidity PeekFirstScalar(
            ReadOnlySpan<Char8> utf8Text,
            out UnicodeScalar scalar,
            out int sequenceLength) => throw null;

        // Similar to PeekFirstScalar, but reads from the end.
        public static SequenceValidity PeekLastScalar(
            ReadOnlySpan<byte> utf8Text,
            out UnicodeScalar scalar,
            out int sequenceLength) => throw null;
        public static SequenceValidity PeekLastScalar(
            ReadOnlySpan<Char8> utf8Text,
            out UnicodeScalar scalar,
            out int sequenceLength) => throw null;

        // Given UTF-8 input text, returns the number of UTF-16 characters and the
        // number of scalars present. Returns false iff the input sequence is invalid
        // and the invalid sequence behavior is to fail.
        public static bool TryGetUtf16CharCount(
            ReadOnlySpan<Char8> utf8Text,
            out int utf16CharCount,
            out int scalarCount,
            InvalidSequenceBehavior replacementBehavior = InvalidSequenceBehavior.ReplaceInvalidSequence) => throw null;

        // Given UTF-16 input text, returns the number of UTF-8 characters and the
        // number of scalars present. Returns false iff the input sequence is invalid
        // and the invalid sequence behavior is to fail *or* if the required number of
        // UTF-8 chars does not fit into an Int32.
        // TODO: Does that mean the 'out' type should be long?
        public static bool TryGetUtf8CharCount(
            ReadOnlySpan<char> utf16Text,
            out int utf8CharCount,
            out int scalarCount,
            InvalidSequenceBehavior replacementBehavior = InvalidSequenceBehavior.ReplaceInvalidSequence) => throw null;

        // Returns the index of the first invalid UTF-8 sequence, or -1 if the input text
        // is well-formed UTF-8. Additionally returns the number of UTF-16 chars and the
        // number of scalars seen up until that point. (If returns -1, the two 'out' values
        // represent the values for the entire string.)
        public static int GetIndexOfFirstInvalidUtf8Sequence(
            ReadOnlySpan<Char8> utf8Text,
            out int utf16CharCount,
            out int scalarCount) => throw null;
    }

    // Allows streaming validation of input.
    // !! MUTABLE STRUCT !!
    public struct Utf8StreamingValidator
    {
        // Returns true iff all data seen up to now represents well-formed UTF-8,
        // or false iff any data consumed so far has an invalid UTF-8 sequence.
        public bool TryConsume(ReadOnlySpan<byte> data, bool isFinalChunk) => throw null;
    }
}
