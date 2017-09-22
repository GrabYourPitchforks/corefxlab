// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Primitives.System.Text.Encoders
{
    /// <summary>
    /// Contains facilities for creating UTF8 byte sequences from Unicode scalar values.
    /// </summary>
    internal static class Utf8Encoder
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEncodeScalar(int scalar, Span<byte> output, out int bytesWritten)
        {
            // This function can encode code points in the range U+0000..U+10FFFF
            // excluding the surrogate range U+D800..U+DFFF.

            uint nonNegativeScalar = (uint)scalar; // convert negative numbers to illegally large positive numbers, caught later
            if (nonNegativeScalar < 0x80U && output.Length != 0)
            {
                // Fast case: simple ASCII value with non-empty output buffer.
                bytesWritten = 1;
                output.DangerousGetPinnableReference() = (byte)nonNegativeScalar;
                return true;
            }
            else
            {
                // Slow case: non-ASCII value *or* invalid value *or* empty output buffer .
                return TryEncodeScalarCore(nonNegativeScalar, ref output.DangerousGetPinnableReference(), output.Length, out bytesWritten);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEncodeScalar2(int scalar, Span<byte> output, out int bytesWritten)
        {
            // This function can encode code points in the range U+0000..U+10FFFF
            // excluding the surrogate range U+D800..U+DFFF.

            uint nonNegativeScalar = (uint)scalar; // convert negative numbers to illegally large positive numbers, caught later
            if (nonNegativeScalar < 0x80U && output.Length != 0)
            {
                // Fast case: simple ASCII value with non-empty output buffer.
                bytesWritten = 1;
                output.DangerousGetPinnableReference() = (byte)nonNegativeScalar;
                return true;
            }
            else
            {
                // Slow case: non-ASCII value *or* invalid value *or* empty output buffer .
                return TryEncodeScalarCore2(nonNegativeScalar, ref output.DangerousGetPinnableReference(), output.Length, out bytesWritten);
            }
        }

        private static bool TryEncodeScalarCore(uint scalar, ref byte output, int outputLength, out int bytesWritten)
        {
            // ASCII scalars should be handled by our caller. If an ASCII scalar found its way to
            // this method, it's because an empty output buffer was provided, and this will be handled
            // at the end of the method.

            if ((scalar < 0x800) && (outputLength >= 2))
            {
                // write two-byte output [ 110yyyyy 10xxxxxx ]
                output = (byte)(0b1100_0000U | (scalar >> 6));
                Unsafe.Add(ref output, 1) = (byte)(0b1000_0000U | (scalar & 0b0011_1111U));
                bytesWritten = 2;
                return true;
            }
            else if ((scalar < 0x10000) && (outputLength >= 3))
            {
                if (!Utf8Decoder.IsSurrogate((int)scalar))
                {
                    // write three-byte output [ 1110zzzz 10yyyyyy 10xxxxxx ]
                    output = (byte)(0b1110_0000U | (scalar >> 12));
                    Unsafe.Add(ref output, 1) = (byte)(0b1000_0000U | ((scalar >> 6) & 0b0011_1111U));
                    Unsafe.Add(ref output, 2) = (byte)(0b1000_0000U | (scalar & 0b0011_1111U));
                    bytesWritten = 3;
                    return true;
                }
            }
            else if ((scalar <= 0x10FFFF) && (outputLength >= 4))
            {
                // write four-byte output [ 11110uuu 10uuzzzz 10yyyyyy 10xxxxxx ]
                output = (byte)(0b1111_0000U | (scalar >> 18));
                Unsafe.Add(ref output, 1) = (byte)(0b1000_0000U | ((scalar >> 12) & 0b0011_1111U));
                Unsafe.Add(ref output, 2) = (byte)(0b1000_0000U | ((scalar >> 6) & 0b0011_1111U));
                Unsafe.Add(ref output, 3) = (byte)(0b1000_0000U | (scalar & 0b0011_1111U));
                bytesWritten = 4;
                return true;
            }

            // If we reached this point, it means one of two things:
            // a) the output buffer was not large enough to hold the encoded value, or
            // b) the scalar was outside the ranges U+0000..U+D7FF and U+E000..U+10FFFF.
            // In either case we fail.

            bytesWritten = 0;
            return false;
        }

        private static bool TryEncodeScalarCore2(uint scalar, ref byte output, int outputLength, out int bytesWritten)
        {
            // ASCII scalars should be handled by our caller. If an ASCII scalar found its way to
            // this method, it's because an empty output buffer was provided, and this will be handled
            // at the end of the method.

            bytesWritten = 0;

            if ((scalar < 0x800) && (outputLength >= 2))
            {
                // write two-byte output [ 110yyyyy 10xxxxxx ]

                output = (byte)(0b1100_0000U | (scalar >> 6));
                goto WriteScalarRemainder1;
            }
            else if ((scalar < 0x10000) && (outputLength >= 3))
            {
                if (!Utf8Decoder.IsSurrogate((int)scalar))
                {
                    // write three-byte output [ 1110zzzz 10yyyyyy 10xxxxxx ]
                    output = (byte)(0b1110_0000U | (scalar >> 12));
                    goto WriteScalarRemainder2;
                }
            }
            else if ((scalar <= 0x10FFFF) && (outputLength >= 4))
            {
                // write four-byte output [ 11110uuu 10uuzzzz 10yyyyyy 10xxxxxx ]
                output = (byte)(0b1111_0000U | (scalar >> 18));
                goto WriteScalarRemainder3;
            }

            // If we reached this point, it means one of two things:
            // a) the output buffer was not large enough to hold the encoded value, or
            // b) the scalar was outside the ranges U+0000..U+D7FF and U+E000..U+10FFFF.
            // In either case we fail.

            return false;

            WriteScalarRemainder3:
            Unsafe.Add(ref output, ++bytesWritten) = (byte)(0b1000_0000U | ((scalar >> 12) & 0b0011_1111U));

            WriteScalarRemainder2:
            Unsafe.Add(ref output, ++bytesWritten) = (byte)(0b1000_0000U | ((scalar >> 6) & 0b0011_1111U));

            WriteScalarRemainder1:
            Unsafe.Add(ref output, ++bytesWritten) = (byte)(0b1000_0000U | (scalar & 0b0011_1111U));

            bytesWritten++;
            return true;
        }
    }
}
