// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System.Text
{
    public static partial class Utf8_
    {
        public static OperationStatus ToUpperInvariant(
            ReadOnlySpan<byte> utf8Input,
            Span<byte> utf8Output,
            out int bytesConsumed,
            out int bytesWritten,
            bool isFinalBlock = true,
            InvalidSequenceBehavior invalidSequenceBehavior = InvalidSequenceBehavior.ReplaceInvalidSequence) => throw null;
        //{
        //    // Parameter checks & default initialization

        //    if (!UnicodeHelpers.IsInRangeInclusive((uint)invalidSequenceBehavior, (uint)InvalidSequenceBehavior.Fail, (uint)InvalidSequenceBehavior.LeaveUnchanged))
        //    {
        //        // TODO: Fix exception message below.
        //        throw new Exception("Bad sequence behavior.");
        //    }

        //    bytesConsumed = 0;
        //    bytesWritten = 0;

        //    // Assuming common case is all-ASCII input, go as far as we can with vector acceleration.
        //    // TODO: This can be optimized by using AVX2 instructions directly rather than generalized
        //    // managed vector operations.

        //    if (Vector.IsHardwareAccelerated)
        //    {
        //        Vector<byte> asciiMask = new Vector<byte>(0x80);
        //        Vector<byte> lowercaseA = new Vector<byte>((byte)'a');
        //        Vector<byte> lowercaseZ = new Vector<byte>((byte)'z');
        //        Vector<byte> changeCaseMask = new Vector<byte>(0x20);

        //        while (utf8Input.Length >= Vector<byte>.Count && utf8Output.Length >= Vector<byte>.Count)
        //        {
        //            // TODO: Remove unsafe code below when necessary Vector APIs come online

        //            var candidate = Unsafe.ReadUnaligned<Vector<byte>>(ref MemoryMarshal.GetReference(utf8Input));
        //            if ((candidate & asciiMask) != Vector<byte>.Zero)
        //            {
        //                break; // non-ASCII data incoming
        //            }

        //            // Change [a-z] to [A-Z], leaving all other bytes the same.
        //            candidate ^= Vector.LessThanOrEqual(candidate - lowercaseA, lowercaseZ) & changeCaseMask;

        //            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(utf8Output), candidate);

        //            utf8Input = utf8Input.Slice(Vector<byte>.Count);
        //            bytesConsumed += Vector<byte>.Count;

        //            utf8Output = utf8Output.Slice(Vector<byte>.Count);
        //            bytesWritten += Vector<byte>.Count;
        //        }
        //    }

        //    // Flush out the last of the ASCII data.

        //    while (!utf8Input.IsEmpty)
        //    {
        //        uint candidate = utf8Input[0];
        //        if (!UnicodeHelpers.IsAsciiCodePoint(candidate))
        //        {
        //            goto HandleNonAsciiData; // non-ASCII data incoming
        //        }

        //        // We know we're going to write a single byte to the destination,
        //        // so we can do a length check immediately.

        //        if (utf8Output.IsEmpty)
        //        {
        //            return OperationStatus.DestinationTooSmall;
        //        }

        //        // Change [a-z] to [A-Z], leaving all other bytes the same.
        //        // TODO: Get JIT to implement this as lea, cmp, setbe, shl, xor
        //        candidate ^= ((candidate - 'a') <= 'z') ? 0x20U : 0;

        //        utf8Output[0] = (byte)candidate;

        //        utf8Input = utf8Input.Slice(1);
        //        bytesConsumed++;

        //        utf8Output = utf8Output.Slice(1);
        //        bytesWritten++;
        //    }

        //    Debug.Assert(utf8Input.IsEmpty, "Should've consumed entire input buffer.");
        //    return OperationStatus.Done;

        //    HandleNonAsciiData:

        //    Debug.Assert(!utf8Input.IsEmpty, "This code path isn't meant for empty input buffers.");
        //    Debug.Assert(!UnicodeHelpers.IsAsciiCodePoint(utf8Input[0]), "Should've flushed all ASCII data.");

        //    // At this point, we know we're working with non-ASCII data, which means we need to
        //    // transcode UTF-8 -> UTF-16, perform the globalization table lookup, then transcode
        //    // back UTF-16 -> UTF-8.

        //    // Bulk transcoding is faster than transcoding individual scalars, so let's try that first.

        //    char[] rentedBuffer = ArrayPool<char>.Shared.Rent(Math.Min(utf8Input.Length, utf8Output.Length));
        //    try
        //    {
        //        // Transcoding by definition cannot leave invalid sequences as-is, so we fudge things
        //        // by telling the transcoder to fail in the face of invalid data, allowing us to copy
        //        // invalid data from the input buffer to the output buffer manually. This only works
        //        // because our transcoding routine is contracted to return the input buffer index where
        //        // the first invalid sequence was seen, so this trick is not generalizable.

        //        InvalidSequenceBehavior transcodeInvalidSequenceBehavior = invalidSequenceBehavior;
        //        if (transcodeInvalidSequenceBehavior == InvalidSequenceBehavior.LeaveUnchanged)
        //        {
        //            transcodeInvalidSequenceBehavior = InvalidSequenceBehavior.Fail;
        //        }

        //        // Don't care about the OperationStatus since we're trying to go as far as possible

        //        TranscodeToUtf16(
        //            utf8Input: utf8Input,
        //            utf16Output: rentedBuffer,
        //            bytesConsumed: out int transcodeBytesConsumed,
        //            charsWritten: out int transcodeCharsWritten,
        //            isFinalBlock: isFinalBlock,
        //            invalidSequenceBehavior: transcodeInvalidSequenceBehavior);

        //        // Uppercase UTF-16 in-place. Per ftp://ftp.unicode.org/Public/UNIDATA/CaseFolding.txt
        //        // and http://www.unicode.org/charts/case/, "simple" case folding (as performed by these
        //        // invariant case conversion routines) will never change the length of a UTF-16 string.
        //        // This also means we don't have to worry about individual code points crossing planes.
        //        // The UTF-16 ToUpperInvariant routine is documented as supporting in-place conversion.

        //        transcodeCharsWritten = new ReadOnlySpan<char>(rentedBuffer, 0, transcodeCharsWritten).ToUpperInvariant(rentedBuffer);

        //        OperationStatus transcodeStatus = TranscodeFromUtf16(
        //            utf16Input: rentedBuffer.AsSpan(0, transcodeCharsWritten),
        //            utf8Output: utf8Output,
        //            charsConsumed: out int transcodeCharsConsumed,
        //            bytesWritten: out int transcodeBytesWritten,
        //            isFinalBlock: true, // since can never end with an incomplete surrogate pair
        //            invalidSequenceBehavior: InvalidSequenceBehavior.Fail); // since can never contain invalid data

        //        Debug.Assert(transcodeStatus != OperationStatus.InvalidData, "Transcode buffer can never contain invalid data.");
        //        Debug.Assert(transcodeStatus != OperationStatus.NeedMoreData, "Transcode buffer can never end with an incomplete surrogate pair.");

        //    }
        //    finally
        //    {
        //        ArrayPool<char>.Shared.Return(rentedBuffer);
        //    }
        //}
    }
}
