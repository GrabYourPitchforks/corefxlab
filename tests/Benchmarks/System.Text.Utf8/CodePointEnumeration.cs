﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Code;

namespace System.Text.Utf8.Benchmarks
{
    public partial class Utf8StringPerf
    {
        [Benchmark]
        [ArgumentsSource(nameof(GetEnumerateCodePointsParameters))]
        public uint EnumerateCodePoints(Utf8String value)
        {
            uint lastValue = default;
            foreach (var codePoint in value)
            {
                lastValue = codePoint;
            }
            return lastValue;
        }

        public IEnumerable<IParam> GetEnumerateCodePointsParameters()
        {
            yield return new EnumerateCodePointsParameter(5, 32, 126, "Short ASCII string");
            yield return new EnumerateCodePointsParameter(5, 32, 0xD7FF, "Short string");
            yield return new EnumerateCodePointsParameter(50000, 32, 126, "Long ASCII string");
            yield return new EnumerateCodePointsParameter(50000, 32, 0xD7FF, "Long string");
        }
        
        public class EnumerateCodePointsParameter : IParam
        {
            private string _value;

            public EnumerateCodePointsParameter(int length, int minCodePoint, int maxCodePoint, string description)
            {
                DisplayText = description;
                _value = GetRandomString(length, minCodePoint, maxCodePoint);
            }

            public string DisplayText { get; }

            public object Value => new Utf8String(_value);

            public string ToSourceCode()
            {
                StringBuilder sb = new StringBuilder("new System.Text.Utf8.Utf8String(\"");
                foreach (char ch in _value)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "\\u{0:X4}", (uint)ch);
                }
                sb.Append("\")");

                return sb.ToString();
            }
        }
    }
}
