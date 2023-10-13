﻿using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoudnessMeterUI.ValueConverters
{
    public class CheckedToRotationConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isExpanded)
                return new RotateTransform(isExpanded ? -90 : 0);

            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
