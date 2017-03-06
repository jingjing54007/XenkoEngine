﻿// Copyright (c) 2014-2017 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Globalization;
using SiliconStudio.Presentation.Internal;

namespace SiliconStudio.Presentation.ValueConverters
{
    /// <summary>
    /// This converter will invert a boolean value. <see cref="ConvertBack"/> is supported and does basically the same operation as <see cref="Convert"/>.
    /// </summary>
    public class InvertBool : ValueConverterBase<InvertBool>
    {
        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = !ConverterHelper.ConvertToBoolean(value, culture);
            return result.Box();
        }

        /// <inheritdoc/>
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }
    }
}
