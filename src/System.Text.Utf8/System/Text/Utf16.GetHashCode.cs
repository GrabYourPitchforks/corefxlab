// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Text
{
    internal static partial class Utf16_
    {
        private const int ArbitraryStackLimit = 512;

        /// <summary>
        /// Returns a hash code for the given UTF-8 string using the specified <see cref="StringComparison"/>.
        /// </summary>
        public static int GetHashCode(
            ReadOnlySpan<char> utf16Input,
            StringComparison comparisonType)
        {
            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return GetHashCode(utf16Input, CultureInfo.CurrentCulture, ignoreCase: false);
                case StringComparison.CurrentCultureIgnoreCase:
                    return GetHashCode(utf16Input, CultureInfo.CurrentCulture, ignoreCase: true);
                case StringComparison.InvariantCulture:
                    return GetHashCode(utf16Input, CultureInfo.InvariantCulture, ignoreCase: false);
                case StringComparison.InvariantCultureIgnoreCase:
                    return GetHashCode(utf16Input, CultureInfo.InvariantCulture, ignoreCase: true);
                case StringComparison.Ordinal:
                    return Marvin.ComputeHash32(MemoryMarshal.AsBytes(utf16Input), Marvin.DefaultSeed);
                case StringComparison.OrdinalIgnoreCase:
                    return GetHashCodeOrdinalIgnoreCase(utf16Input);
            }

            // TODO: Fix exception message below.
            throw new Exception("Bad comparison type.");
        }

        /// <summary>
        /// Returns a culture-aware hash code for the given UTF-8 string using the specified <see cref="CultureInfo"/>
        /// and case sensitivity settings.
        /// </summary>
        public static int GetHashCode(
            ReadOnlySpan<char> utf16Input,
            CultureInfo cultureInfo,
            bool ignoreCase)
        {
            if (cultureInfo == null)
            {
                throw new ArgumentNullException(nameof(cultureInfo));
            }

            // TODO: Make this allocation-free. Currently the only APIs available to us from CultureInfo work on
            // String, not ReadOnlySpan<char>.

            return cultureInfo.CompareInfo.GetHashCode(
                new string(utf16Input),
                (ignoreCase) ? CompareOptions.IgnoreCase : CompareOptions.None);
        }

        private static int GetHashCodeOrdinalIgnoreCase(ReadOnlySpan<char> utf16Input)
        {
            // Just like String.GetHashCode(StringComparison.OrdinalIgnoreCase),
            // we'll be implemented as the hash code over the upper invariant form of the
            // input string.

            char[] rentedChars = null;
            Span<char> tempBuffer = (utf16Input.Length > ArbitraryStackLimit)
                ? (rentedChars = ArrayPool<char>.Shared.Rent(utf16Input.Length))
                : stackalloc char[ArbitraryStackLimit];

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

            int actualBufferSize = utf16Input.ToUpperInvariant(tempBuffer);
            int hashCode = Marvin.ComputeHash32(MemoryMarshal.AsBytes(tempBuffer.Slice(0, actualBufferSize)), Marvin.DefaultSeed);

            if (rentedChars != null)
            {
                ArrayPool<char>.Shared.Return(rentedChars);
            }

            return hashCode;
        }
    }
}
