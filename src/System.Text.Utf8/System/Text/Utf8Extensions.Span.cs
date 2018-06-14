// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

using Char8 = System.Text.Utf8Char;

namespace System.Text
{
    // TODO: Should there also be overloads that take 'this Span<Char8>' for mutable buffers?
    public static partial class Utf8Extensions
    {
        // Named like the String.CompareTo instance method, but has an API surface closer to the String.Compare static method
        public static int CompareTo(this ReadOnlySpan<Char8> utf8Text, ReadOnlySpan<Char8> other, CultureInfo culture, CompareOptions options) => throw null;
        public static int CompareTo(this ReadOnlySpan<Char8> utf8Text, ReadOnlySpan<Char8> other, StringComparison comparisonType) => throw null;

        public static void Contains(this ReadOnlySpan<Char8> utf8Text, ReadOnlySpan<Char8> value) => throw null;
        public static void Contains(this ReadOnlySpan<Char8> utf8Text, ReadOnlySpan<Char8> value, StringComparison comparisonType) => throw null;
        public static void Contains(this ReadOnlySpan<Char8> utf8Text, UnicodeScalar value) => throw null;
        public static void Contains(this ReadOnlySpan<Char8> utf8Text, UnicodeScalar value, StringComparison comparisonType) => throw null;

        // This API is useful, but perhaps best suited as a normal static method on a helper type rather than as an extension method?
        public static int GetHashCode(this ReadOnlySpan<Char8> utf8Text, StringComparison comparisonType) => throw null;

        // Also EndsWith
        // n.b. The method below masks MemoryExtensions.StartsWith<T>(this ROS<T>, ROS<T>) because we want to
        // use the proper culture. This same logic *does not* exist for string segments, which means String.StartsWith
        // and String.Span.StartsWith use different culture comparisons by default. We should address this.
        public static bool StartsWith(this ReadOnlySpan<Char8> utf8Text, ReadOnlySpan<Char8> value) => throw null;
        public static bool StartsWith(this ReadOnlySpan<Char8> utf8Text, ReadOnlySpan<Char8> value, StringComparison comparisonType) => throw null;

        // First overload is the same as SequenceEqual, but calling it 'Equals' for discoverability.
        public static bool Equals(this ReadOnlySpan<Char8> utf8Text, ReadOnlySpan<Char8> value) => throw null;
        public static bool Equals(this ReadOnlySpan<Char8> utf8Text, ReadOnlySpan<Char8> value, StringComparison stringComparison) => throw null;

        // Also LastIndexOf{Any}
        // Same deal as StartsWith; the first overload masks the normal extension method on MemoryExtensions.
        public static int IndexOf(this ReadOnlySpan<Char8> utf8Text, ReadOnlySpan<Char8> value) => throw null;
        public static int IndexOf(this ReadOnlySpan<Char8> utf8Text, ReadOnlySpan<Char8> value, StringComparison stringComparison) => throw null;
        public static int IndexOf(this ReadOnlySpan<Char8> utf8Text, UnicodeScalar value) => throw null;
        public static int IndexOf(this ReadOnlySpan<Char8> utf8Text, UnicodeScalar value, StringComparison stringComparison) => throw null;
        public static int IndexOfAny(this ReadOnlySpan<Char8> utf8Text, ReadOnlySpan<UnicodeScalar> values, StringComparison stringComparison) => throw null;

        public static bool IsEmptyOrWhiteSpace(this ReadOnlySpan<Char8> utf8Text) => throw null;
        
        // Also TrimStart, TrimEnd
        public static ReadOnlySpan<Char8> Trim(this ReadOnlySpan<Char8> utf8Text) => throw null;
        public static ReadOnlySpan<Char8> Trim(this ReadOnlySpan<Char8> utf8Text, UnicodeScalar trimScalar) => throw null;
        public static ReadOnlySpan<Char8> Trim(this ReadOnlySpan<Char8> utf8Text, ReadOnlySpan<UnicodeScalar> trimScalars) => throw null;

        // Also TryToLower, TryToLowerInvariant
        // 'out' value is total output buffer size required (if returns false) or actual output elements written (if returns true)
        // Open question: should this instead be an OperationStatus-style API?
        // If so, it would look something like this:
        // public static OperationStatus ToUpper(ReadOnlySpan<Char8> input, Span<Char8> output, out int inputElementsRead, out int outputElementsWritten, bool isFinalChunk = true);
        public static bool TryToUpper(this ReadOnlySpan<Char8> utf8Text, Span<Char8> output, CultureInfo culture, out int outputElementCount) => throw null;
        public static bool TryToUpperInvariant(this ReadOnlySpan<Char8> utf8Text, Span<Char8> output, out int outputElementCount) => throw null;

        public static Utf8String ToUtf8String(this ReadOnlySpan<Char8> utf8Text) => throw null;

        // This would actually be an instance method on ROS<T>, but including here for API completeness.
        public static string ToString(this ReadOnlySpan<Char8> utf8Text) => throw null;

        // Like TryToUpper, this could also be an OperationStatus-style API.
        public static bool TryToString(this ReadOnlySpan<Char8> utf8Text, Span<char> output, out int outputElementCount) => throw null;

        /*
         * ENUMERATION
         */

        // Ideally this is an extension property ReadOnlySpan<Char8>.Scalars
        // Also accessible via Utf8.GetScalars(ReadOnlySpan<Char8>)
        public static Utf8SpanUnicodeScalarEnumerator get_Scalars(this ReadOnlySpan<Char8> utf8Text) => throw null;
        
        /*
         * PROJECTION APIS
         */

        // O(1) - this is just an unsafe cast
        public static ReadOnlySpan<byte> AsBytes(this ReadOnlySpan<Char8> utf8Text) => throw null;

        // O(1) - this is just an unsafe cast (non-validating)
        public static ReadOnlySpan<Char8> AsUtf8(this ReadOnlySpan<byte> utf8Text) => throw null;
    }
}
