﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Code;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Utf8StringRealType = System.Text.Utf8.Utf8String;

namespace Benchmarks.Utf8String
{
    public partial class Utf8String
    {
        [Benchmark]
        [ArgumentsSource(nameof(GetConstructFromStringParameters))]
        public Utf8StringRealType ConstructFromString(string value)
        {
            return new Utf8StringRealType(value);
        }

        public IEnumerable<IParam> GetConstructFromStringParameters()
        {
            yield return new ConstructFromStringParameter(5, 32, 126, "Short ASCII string");
            yield return new ConstructFromStringParameter(5, 32, 0xD7FF, "Short string");
            yield return new ConstructFromStringParameter(50000, 32, 126, "Long ASCII string");
            yield return new ConstructFromStringParameter(50000, 32, 0xD7FF, "Long string");
        }

        private static string GetRandomString(int length, int minCodePoint, int maxCodePoint)
        {
            Random r = new Random(42);
            StringBuilder sb = new StringBuilder(length);
            while (length-- != 0)
            {
                sb.Append((char)r.Next(minCodePoint, maxCodePoint));
            }
            return sb.ToString();
        }

        public class ConstructFromStringParameter : IParam
        {
            public ConstructFromStringParameter(int length, int minCodePoint, int maxCodePoint, string description)
            {
                DisplayText = description;
                Value = GetRandomString(length, minCodePoint, maxCodePoint);
            }

            public string DisplayText { get; }

            public object Value { get; }

            public string ToSourceCode()
            {
                string valueAsString = (string)Value;

                StringBuilder sb = new StringBuilder("\"");
                foreach (char ch in valueAsString)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "\\u{0:X4}", (uint)ch);
                }
                sb.Append("\"");

                return sb.ToString();
            }
        }
    }
}
