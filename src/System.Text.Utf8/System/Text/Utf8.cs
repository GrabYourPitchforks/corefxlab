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
                    return GetHashCode(utf8Input, CultureInfo.CurrentCulture, ignoreCase: true);
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

            // First, try converting as many ASCII bytes as we can without involving transcoding.

            if (!utf8Input.IsEmpty)
            {
                byte[] rentedBytes = null;
                Span<byte> tempBuffer = (utf8Input.Length > ArbitraryStackLimit)
                    ? (rentedBytes = ArrayPool<byte>.Shared.Rent(utf8Input.Length))
                    : stackalloc byte[ArbitraryStackLimit];

                int numBytesCopied = ChangeCaseAscii(utf8Input, rentedBytes, toUpper: true);
                marvin.Consume(rentedBytes.AsSpan(0, numBytesCopied));
                utf8Input = utf8Input.Slice(numBytesCopied);

                if (rentedBytes != null)
                {
                    ArrayPool<byte>.Shared.Return(rentedBytes);
                }
            }

            // If there are any non-ASCII bytes remaining, process them now.
            // This involves a transcoding (UTF-8 -> UTF-16) step.

            if (!utf8Input.IsEmpty)
            {
                // Emit a sentinel value indicating that we're switching from UTF-8 to UTF-16 operation.
                // This is helpful because it allows us to distinguish [ 58 58 ] (a valid 2-byte UTF-8
                // sequence) from [ E5 A1 98 ] (a valid 3-byte UTF-8 sequence which represents the UTF-16
                // sequence [ 5858 ]), avoiding trivial hash code collisions.

                marvin.Consume<uint>(0x00FF00FF);

                Span<char> utf16Buffer = stackalloc char[ArbitraryStackLimit];

                do
                {
                    // Normally the transcoding process will fix up invalid sequences, replacing them with U+FFFD.
                    // We don't want this behavior because it leads to trivial hash collisions in the input domain,
                    // where lots of input strings which differ only in a single invalid byte sequence won't compare
                    // as equal but will result in the same hash code. We work around this by identifying invalid
                    // UTF-8 sequences and converting them to invalid UTF-16 sequences (which transcoding would normally
                    // never produce on its own). This allows us to propagate invalid input in a loose sense and
                    // ensure that unique invalid UTF-8 byte sequences contribute to the final hash output.

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
                    //
                    // Normally retrieving a hash code from a string involves having the entire string available
                    // as a single block, as the German Eszett (one Unicode scalar value) needs to compare as
                    // equivalent to the two-scalar string "ss". The OrdinalIgnoreCase comparison is special
                    // since it treats each scalar as completely standalone, so we can process the uppercase
                    // conversion in isolated chunks. Culture-sensitive conversions cannot use this same trick.

                    var uppercaseSlice = utf16Buffer.Slice(0, ((ReadOnlySpan<char>)utf16Buffer).ToUpperInvariant(utf16Buffer));
                    marvin.Consume(MemoryMarshal.AsBytes(uppercaseSlice));
                    utf8Input = utf8Input.Slice(0, bytesConsumed);

                    Debug.Assert(operationStatus != OperationStatus.NeedMoreData, "Cannot occur if isFinalBlock = true");

                    if (operationStatus == OperationStatus.InvalidData)
                    {
                        // Our transcoder's contract is that in the event of failure, 'bytesConsumed' contains
                        // the index of the first byte of the first invalid UTF-8 sequence. We can leverage this
                        // to easily inspect and manipulate the invalid sequence. Not every OperationStatus-returning
                        // method has this same contract so this trick isn't generalizable.

                        var result = UnicodeReader.PeekFirstScalarUtf8(utf8Input);
                        Debug.Assert(result.status == SequenceValidity.InvalidSequence, "InvalidData should be accompanied by an invalid sequence.");
                        Debug.Assert(result.charsConsumed >= 1 && result.charsConsumed <= 3, "Expected 1 - 3 bytes consumed.");

                        for (int i = 0; i < result.charsConsumed; i++)
                        {
                            // Invalid UTF-8 sequences are converted to the invalid UTF-16 sequence [ DD## ].
                            // This is an isolated low surrogate code point, which is always invalid UTF-16,
                            // so it's distinct from all other transcoder output.
                            //
                            // We prefer to consume these invalid UTF-16 sequences rather than writing the
                            // invalid UTF-8 sequence directly, as invalid UTF-8 sequences can look like valid
                            // UTF-16 sequences. Consider [ D8 D8 DF DF ] (invalid UTF-8) and [ F1 86 8F 9F ]
                            // (valid UTF-8 whose UTF-16 representation is [ D8D8 DFDF ]). This demonstrates
                            // how writing the invalid UTF-8 sequences directly can lead to trivial hash code
                            // collisions.

                            marvin.Consume((ushort)(utf8Input[i] + 0xDD00));
                        }

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
