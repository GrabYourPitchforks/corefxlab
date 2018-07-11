// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

//
// !! TODO: Remove this when Marvin is made public.
// This file is copied from corefx/src/Common/src/System/Marvin.cs, commit 540a99c on Jan 29, 2018,
// with a slight modification to the default seed.
//

namespace System
{
    internal struct StreamingMarvin
    {
        public static StreamingMarvin CreateForUtf8() => throw null;

        public void Consume(ReadOnlySpan<byte> contents) => throw null;

        public int Finish() => throw null;
    }
}
