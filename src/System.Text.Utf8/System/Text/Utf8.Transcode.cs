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
    }
}
