// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System.Text
{
    internal static partial class Utf16_
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
            ReadOnlySpan<char> input,
            Span<char> output,
            bool toUpper)
        {
            // Requirement: output must be large enough to hold input.
            // (Though we'll truncate if needed so that we don't overrun the buffer.)

            Debug.Assert(output.Length >= input.Length, "Output buffer too small.");

            ref char firstNonAsciiInputChar = ref ChangeCaseAsciiCore(
                input: ref MemoryMarshal.GetReference(input),
                output: ref MemoryMarshal.GetReference(output),
                charCount: Math.Min(input.Length, output.Length),
                toUpper: toUpper);

            return (int)((nuint)Unsafe.ByteOffset(ref MemoryMarshal.GetReference(input), ref firstNonAsciiInputChar) / sizeof(char));
        }

        // Returns a reference to the first non-ASCII input character (or to the element just
        // past the end of the buffer if all input characters were ASCII).
        private static ref char ChangeCaseAsciiCore(
            ref char input,
            ref char output,
            nuint charCount,
            bool toUpper)
        {
            // Cannot use Vector<char>, so use Vector<ushort> instead.
            Debug.Assert(sizeof(char) == sizeof(ushort));

            if (Vector.IsHardwareAccelerated && charCount >= Vector<ushort>.Count)
            {
                var containsNonAsciiCharsMask = new Vector<ushort>(unchecked((ushort)(~0x7F)));
                var endOfAsciiRange = new Vector<ushort>(0x7F);
                var flipCaseMask = new Vector<ushort>(0x20);
                var endOfAlphaRange = new Vector<ushort>(25);
                var startOfAlphaRange = new Vector<ushort>('A');
                if (toUpper)
                {
                    startOfAlphaRange ^= flipCaseMask;
                }

                ref char lastAddrWhereCanReadVector = ref Unsafe.Add(ref Unsafe.Add(ref input, charCount), -Vector<ushort>.Count);

                do
                {
                    do
                    {
                        // Read a vector, change case, and write it back
                        // Non-ASCII characters remain unmodified

                        var original = Unsafe.ReadUnaligned<Vector<ushort>>(ref Unsafe.As<char, byte>(ref input));
                        var modified = original ^ (Vector.LessThanOrEqual(original - startOfAlphaRange, endOfAlphaRange) & flipCaseMask);
                        Unsafe.WriteUnaligned(ref Unsafe.As<char, byte>(ref output), modified);

                        // Were there any non-ASCII characters? If so, figure out which index
                        // contained the non-ASCII character and return it now.

                        if ((original & containsNonAsciiCharsMask) != Vector<ushort>.Zero)
                        {
                            // Create a vector where the non-ASCII elements are all 1 and the ASCII elements are all 0
                            var nonAsciiChars = Vector.GreaterThan(original, endOfAsciiRange);

                            // If AVX/AVX2 is supported and vectors are 256 bits, we can use pmovmskb, lzcnt to quickly
                            // calculate how many bytes were ASCII.

                            if (Avx.IsSupported && Avx2.IsSupported && Unsafe.SizeOf<Vector<ushort>>() == Unsafe.SizeOf<Vector256<ushort>>() && Lzcnt.IsSupported)
                            {
                                var mask = Avx2.MoveMask(Avx.StaticCast<ushort, byte>(Unsafe.As<Vector<ushort>, Vector256<ushort>>(ref nonAsciiChars)));
                                return ref Unsafe.AddByteOffset(ref input, (nuint)Lzcnt.LeadingZeroCount((uint)mask));
                            }

                            // If AVX/AVX2 is not supported, fall back to a standard loop.

                            int i = 0;
                            for (; i < Vector<ushort>.Count && nonAsciiChars[i] == 0; i++) { }
                            return ref Unsafe.Add(ref input, i);
                        }

                        // At this point, we know all characters were ASCII, so continue the loop.

                        input = ref Unsafe.Add(ref input, Vector<ushort>.Count);
                        output = ref Unsafe.Add(ref output, Vector<ushort>.Count);
                    } while (!Unsafe.IsAddressGreaterThan(ref input, ref lastAddrWhereCanReadVector));

                    // At this point, we've not encountered any non-ASCII data. If the input buffer was an
                    // exact multiple of the vector size, then there's no more data and we can return. If
                    // there's any existing data in the input buffer, back up a bit so that we can perform
                    // one final vector read and check.

                    if (!Unsafe.AreSame(ref input, ref Unsafe.Add(ref lastAddrWhereCanReadVector, Vector<ushort>.Count)))
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
                    Unsafe.Add(ref output, i) = (char)thisChar;
                }

                return ref Unsafe.Add(ref input, i);
            }
        }
    }
}
