// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System.Text
{
    public static partial class Utf8_
    {
        /// <summary>
        /// Copies data from the input buffer to the output buffer, changing the case of any ASCII
        /// characters encountered along the way. Terminates when a non-ASCII character is seen.
        /// Returns the number of ASCII characters copied.
        /// </summary>
        /// <remarks>
        /// Caller should ensure output buffer is at least as large as input buffer. On method return,
        /// the contents of the output buffer beyond the return value index are undefined.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int ChangeCaseAscii(
            ReadOnlySpan<byte> input,
            Span<byte> output,
            bool toUpper)
        {
            // Requirement: output must be large enough to hold input.
            // (Though we'll truncate if needed so that we don't overrun the buffer.)

            Debug.Assert(output.Length >= input.Length, "Output buffer too small.");

            ref byte firstNonAsciiInputChar = ref ChangeCaseAsciiCore(
                input: ref MemoryMarshal.GetReference(input),
                output: ref MemoryMarshal.GetReference(output),
                charCount: Math.Min(input.Length, output.Length),
                toUpper: toUpper);

            return (int)(nuint)Unsafe.ByteOffset(ref MemoryMarshal.GetReference(input), ref firstNonAsciiInputChar);
        }

        // Returns a reference to the first non-ASCII input character (or to the element just
        // past the end of the buffer if all input characters were ASCII).
        private static ref byte ChangeCaseAsciiCore(
            ref byte input,
            ref byte output,
            nuint charCount,
            bool toUpper)
        {
            if (Avx.IsSupported && Avx2.IsSupported && Lzcnt.IsSupported && charCount >= Unsafe.SizeOf<Vector256<sbyte>>())
            {
                // Since we only have vpcmpgtb (signed) comparison checks, we need 'start' to be just before 'A'
                // and 'end' to be right at 'Z'. When AVX512 support comes online we'll be able to use unsigned
                // comparisons via the vpcmpub instruction.

                var flipCaseMask = Avx.SetAllVector256<sbyte>(0x20);
                var startOfAlphaRange = Avx.SetAllVector256<sbyte>('A' - 1);
                if (toUpper)
                {
                    startOfAlphaRange = Avx2.Xor(startOfAlphaRange, flipCaseMask);
                }
                var endOfAlphaRange = Avx2.Add(startOfAlphaRange, Avx.SetAllVector256<sbyte>(26));

                ref byte lastAddrWhereCanReadVector = ref Unsafe.Add(ref Unsafe.Add(ref input, charCount), -Unsafe.SizeOf<Vector256<sbyte>>());

                do
                {
                    do
                    {
                        // Read a vector, change case, and write it back
                        // Non-ASCII characters remain unmodified

                        var original = Unsafe.ReadUnaligned<Vector256<sbyte>>(ref input);

                        // toModifyMask = ~(endOfAlphaRange > original) & (original > startOfAlphaRange);
                        // Each element of toModifyMask will be FFh or 00h.

                        var toModifyMask = Avx2.AndNot(
                            Avx2.CompareGreaterThan(endOfAlphaRange, original),
                            Avx2.CompareGreaterThan(original, startOfAlphaRange));

                        var modified = Avx2.Xor(original, Avx2.And(toModifyMask, flipCaseMask));
                        Unsafe.WriteUnaligned(ref output, modified);

                        // Were there any non-ASCII characters? If so, figure out which index
                        // contained the non-ASCII character and return it now. We can use pmovmskb, lzcnt
                        // to quickly calculate how many bytes were ASCII.

                        var mask = (uint)Avx2.MoveMask(original);
                        if (mask != 0)
                        {
                            return ref Unsafe.Add(ref input, (int)Lzcnt.LeadingZeroCount(mask));
                        }

                        // At this point, we know all characters were ASCII, so continue the loop.

                        input = ref Unsafe.Add(ref input, Unsafe.SizeOf<Vector256<sbyte>>());
                        output = ref Unsafe.Add(ref output, Unsafe.SizeOf<Vector256<sbyte>>());
                    } while (!Unsafe.IsAddressGreaterThan(ref input, ref lastAddrWhereCanReadVector));

                    // At this point, we've not encountered any non-ASCII data. If the input buffer was an
                    // exact multiple of the vector size, then there's no more data and we can return. If
                    // there's any existing data in the input buffer, back up a bit so that we can perform
                    // one final vector read and check.

                    if (!Unsafe.AreSame(ref input, ref Unsafe.Add(ref lastAddrWhereCanReadVector, Unsafe.SizeOf<Vector256<sbyte>>())))
                    {
                        output = ref Unsafe.AddByteOffset(ref output, Unsafe.ByteOffset(ref input, ref lastAddrWhereCanReadVector));
                        input = ref lastAddrWhereCanReadVector;
                        continue;
                    }
                } while (false);
                return ref input;
            }
            else
            {
                // If this code path is hit, either vectorization is unavailable or the input buffer
                // is smaller than the vector size.

                // TODO: Generalized vectorization not dependent on AVX2.
                // TODO: Poor man's vectorization.

                uint startOfAsciiRange = (toUpper) ? 'a' : 'A';

                nuint i = 0;
                for (; i < charCount; i++)
                {
                    uint thisChar = Unsafe.Add(ref input, i);
                    if (thisChar >= 0x7F)
                    {
                        break; // non-ASCII data incoming
                    }

                    // It's ok for the check above to have a branch since we expect it to very rarely
                    // get taken, but the code below should be branchless since we expect case changes
                    // to be a frequent but unpredictable occurrence.

                    bool needsCaseChange = (thisChar - startOfAsciiRange) <= (uint)('z' - 'a');
                    thisChar ^= (uint)(Unsafe.As<bool, byte>(ref needsCaseChange) << 5);
                    Unsafe.Add(ref output, i) = (byte)thisChar;
                }

                return ref Unsafe.Add(ref input, i);
            }
        }
    }
}
