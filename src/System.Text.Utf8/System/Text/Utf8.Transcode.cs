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
        public static OperationStatus TranscodeFromUtf16(
            ReadOnlySpan<char> utf16Input,
            Span<byte> utf8Output,
            out int charsConsumed,
            out int bytesWritten,
            bool isFinalBlock = true,
            InvalidSequenceBehavior invalidSequenceBehavior = InvalidSequenceBehavior.ReplaceInvalidSequence) => throw null;

        public static OperationStatus TranscodeToUtf16(
            ReadOnlySpan<byte> utf8Input,
            Span<char> utf16Output,
            out int bytesConsumed,
            out int charsWritten,
            bool isFinalBlock = true,
            InvalidSequenceBehavior invalidSequenceBehavior = InvalidSequenceBehavior.ReplaceInvalidSequence) => throw null;

        /// <summary>
        /// Transcodes UTF-8 to UTF-16, but the individual bytes of invalid UTF-8 sequences are transcoded
        /// as the invalid UTF-16 sequence [ DD## ], where "##" is the UTF-8 code unit.
        /// </summary>
        /// <returns>
        /// The number of chars written to the destination.
        /// </returns>
        /// <remarks>
        /// Cannot throw as long as the output buffer has at least as many elements at the input buffer.
        /// See comments in <see cref="GetHashCodeOrdinalIgnoreCase(ReadOnlySpan{byte})"/> for why this method exists.
        /// </remarks>
        internal static int TranscodeToUtf16PropagateMalformedSequences(
            ReadOnlySpan<byte> utf8Input,
            Span<char> utf16Output)
        {
            Debug.Assert(utf16Output.Length >= utf8Input.Length, "Bad output buffer size specified.");

            int retVal = 0;

            while (true)
            {
                // First, go as far as we can.
                // We specify 'Fail' below so that we can perform manual error handling.

                OperationStatus operationStatus = TranscodeToUtf16(
                    utf8Input: utf8Input,
                    utf16Output: utf16Output,
                    bytesConsumed: out int bytesConsumed,
                    charsWritten: out int charsWritten,
                    isFinalBlock: true,
                    invalidSequenceBehavior: InvalidSequenceBehavior.Fail);

                Debug.Assert(operationStatus != OperationStatus.NeedMoreData, "Cannot have this status with isFinalBlock = true.");
                Debug.Assert(operationStatus != OperationStatus.DestinationTooSmall, "Transcoding UTF-8 -> UTF-16 without replacement cannot increase code unit count.");

                retVal += charsWritten;
                if (operationStatus == OperationStatus.Done)
                {
                    return retVal;
                }

                // If we reached this point, an invalid sequence was encountered.
                // Copy each byte of the invalid sequence as [ DD## ], which is invalid UTF-16.
                // Then go back to the beginning of the loop.

                utf8Input = utf8Input.Slice(bytesConsumed);
                utf16Output = utf16Output.Slice(charsWritten);

                var result = UnicodeReader.PeekFirstScalarUtf8(utf8Input);
                Debug.Assert(result.status == SequenceValidity.InvalidSequence, "InvalidData should be accompanied by an invalid sequence.");

                for (int i = 0; i < result.charsConsumed; i++)
                {
                   utf16Output[i] = (char)(utf8Input[i] + 0xDD00);
                }

                utf8Input = utf8Input.Slice(result.charsConsumed);
                utf16Output = utf16Output.Slice(result.charsConsumed);
            }
        }
    }
}
