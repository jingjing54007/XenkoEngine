﻿// Copyright (c) 2014-2017 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Globalization;
using SiliconStudio.Presentation.Internal;

namespace SiliconStudio.Presentation.ValueConverters
{
    /// <summary>
    /// This converter will convert a numerical value to a boolean. The result will be <c>false</c> if the given value is equal to zero, <c>true</c> otherwise.
    /// </summary>
    /// <remarks>Supported types are: <see cref="SByte"/>, <see cref="Int16"/>, <see cref="Int32"/>, <see cref="Int64"/>, <see cref="Byte"/>, <see cref="UInt16"/>, <see cref="UInt32"/>, <see cref="UInt64"/></remarks>
    public class NumericToBool : OneWayValueConverter<NumericToBool>
    {
        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result;
            if (value is sbyte) result = (sbyte)value != 0;
            else if (value is short) result = (short)value != 0;
            else if (value is int) result = (int)value != 0;
            else if (value is long) result = (long)value != 0;
            else if (value is byte) result = (byte)value != 0;
            else if (value is ushort) result = (ushort)value != 0;
            else if (value is uint) result = (uint)value != 0;
            else if (value is ulong) result = (ulong)value != 0;
            else
                throw new ArgumentException($"{nameof(value)} is not a numeric type");
            return result.Box();
        }
    }
}
