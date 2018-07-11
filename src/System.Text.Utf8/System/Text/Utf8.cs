// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Text
{
    public static partial class Utf8_
    {
        private const int ArbitraryStackLimit = 512;

        /// <summary>
        /// Returns a hash code for the given UTF-8 string using the specified <see cref="StringComparison"/>.
        /// </summary>
        public static int GetHashCode(
            ReadOnlySpan<byte> utf8Input,
            StringComparison comparisonType)
        {
            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return GetHashCode(utf8Input, CultureInfo.CurrentCulture, ignoreCase: false);
                case StringComparison.CurrentCultureIgnoreCase:
                    return GetHashCode(utf8Input, CultureInfo.InvariantCulture, ignoreCase: true);
                case StringComparison.InvariantCulture:
                    return GetHashCode(utf8Input, CultureInfo.InvariantCulture, ignoreCase: false);
                case StringComparison.InvariantCultureIgnoreCase:
                    return GetHashCode(utf8Input, CultureInfo.InvariantCulture, ignoreCase: true);
                case StringComparison.Ordinal:
                    return Marvin.ComputeHash32(utf8Input, Marvin.Utf8StringSeed);
                case StringComparison.OrdinalIgnoreCase:
                    return GetHashCodeOrdinalIgnoreCase(utf8Input);
            }

            // TODO: Fix exception message below.
            throw new Exception("Bad comparison type.");
        }

        private static int GetHashCodeOrdinalIgnoreCase(ReadOnlySpan<byte> utf8Input)
        {
            // Just like String.GetHashCode(StringComparison.OrdinalIgnoreCase),
            // we'll be implemented as the hash code over the upper invariant form of the
            // input string.

            StreamingMarvin marvin = StreamingMarvin.CreateForUtf8();

            if (!utf8Input.IsEmpty)
            {
                // TODO: Provide a more optimized implementation using vector acceleration
                // and avoiding the transcoding step for the common case of ASCII input data.

                Span<char> utf16Buffer = stackalloc char[ArbitraryStackLimit];

                do
                {
                    // We cannot pass InvalidSequenceBehavior.LeaveUnchanged to the transcoder because
                    // we are by definition changing the representation of the underlying data. Instead
                    // we fudge things by telling the transcoder to fail in the face of invalid data,
                    // allowing us to copy invalid data from the input buffer to the output buffer manually.
                    // This only works because our transcoding routine is contracted to return the input
                    // buffer index where the first invalid sequence was seen, so this trick is not generalizable.

                    OperationStatus operationStatus = TranscodeToUtf16(
                        utf8Input: utf8Input,
                        utf16Output: utf16Buffer,
                        bytesConsumed: out int bytesConsumed,
                        charsWritten: out int charsWritten,
                        isFinalBlock: true /* hash code is over entire buffer */,
                        invalidSequenceBehavior: InvalidSequenceBehavior.Fail);

                    // Uppercase UTF-16 in-place. Per ftp://ftp.unicode.org/Public/UNIDATA/CaseFolding.txt
                    // and http://www.unicode.org/charts/case/, "simple" case folding (as performed by these
                    // invariant case conversion routines) will never change the length of a UTF-16 string.
                    // This also means we don't have to worry about individual code points crossing planes.
                    // The UTF-16 ToUpperInvariant routine is documented as supporting in-place conversion.

                    var uppercaseSlice = utf16Buffer.Slice(0, ((ReadOnlySpan<char>)utf16Buffer).ToUpperInvariant(utf16Buffer));
                    marvin.Consume(MemoryMarshal.AsBytes(uppercaseSlice));
                    utf8Input = utf8Input.Slice(0, bytesConsumed);

                    Debug.Assert(operationStatus != OperationStatus.NeedMoreData, "Cannot occur if isFinalBlock = true");

                    if (operationStatus == OperationStatus.InvalidData)
                    {
                        var result = UnicodeReader.PeekFirstScalarUtf8(utf8Input);
                        Debug.Assert(result.status == SequenceValidity.InvalidSequence, "InvalidData should be accompanied by an invalid sequence.");

                        // Copy invalid bytes as-is to the hash routine, then continue. The reason we do this
                        // is that we don't ever want two distinct invalid UTF-8 strings to compare as equal,
                        // which means that we also want their hash codes to be distinct, ensured by mixing
                        // the invalid bytes directly into the hash code. A naive hash code implementation
                        // where invalid sequences are replaced with U+FFFD could lead to a bunch of inequal
                        // strings all having the same hash code, which could subject the application to a
                        // denial of service attack.

                        // We also need a way to distinguish invalid UTF-8 bytes from valid UTF-16 code units,
                        // otherwise the underlying hash code can't tell the difference between [ D8 D8 DF DF ]
                        // (which is invalid UTF-8) and [ F1 86 8F 9F ] (which is U+463DF, whose UTF-16 representation
                        // is [ D8D8 DFDF ]), which allows trivial collisions. We can address this by using
                        // [ DD DD ] as a sentinel to indicate that the next several bytes are bad UTF-8, not
                        // good UTF-16. Since [ DD DD ] can never appear unless immediately after a high surrogate,
                        // this effectively disambiguates the two cases. We'll also include the length of the invalid
                        // sequence as an additional discriminator to help differentiate between [ F3 80 80 ] (an
                        // 3-byte invalid UTF-8 sequence) and [ F3 E8 82 80 ] (a 1-byte invalid UTF-8 sequence followed
                        // by a 3-byte valid UTF-8 sequence corresponding to UTF-16 [ 8080 ]).

                        Debug.Assert(result.charsConsumed >= 1 && result.charsConsumed <= 3, "Expected 1 - 3 bytes consumed.");

                        uint sentinel = (uint)(result.charsConsumed + 0xDDDD0000);
                        marvin.Consume(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref sentinel, 1)));
                        marvin.Consume(utf8Input.Slice(0, result.charsConsumed));
                        utf8Input = utf8Input.Slice(result.charsConsumed);
                    }
                } while (!utf8Input.IsEmpty);
            }

            return marvin.Finish();
        }

        /// <summary>
        /// Returns a culture-aware hash code for the given UTF-8 string using the specified <see cref="CultureInfo"/>
        /// and case sensitivity settings.
        /// </summary>
        public static int GetHashCode(
            ReadOnlySpan<byte> utf8Input,
            CultureInfo cultureInfo,
            bool ignoreCase) => throw null;
    }
}
