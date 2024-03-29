﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BowieD.MusicPlayer.WPF.Extensions
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool vb)
                return !vb;

            throw new InvalidCastException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool vb)
                return !vb;

            throw new InvalidCastException();
        }
    }
    public class BooleanConverter<T> : IValueConverter
    {
        public BooleanConverter(T trueValue, T falseValue)
        {
            this.True = trueValue;
            this.False = falseValue;
        }

        public T True { get; set; }
        public T False { get; set; }

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue && boolValue ? True : False;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is T tValue && EqualityComparer<T>.Default.Equals(tValue, True);
        }
    }

    public sealed class CustomBooleanToVisibilityConverter : BooleanConverter<Visibility>
    {
        public CustomBooleanToVisibilityConverter() : base(Visibility.Visible, Visibility.Collapsed) { }
    }
    public sealed class CustomBooleanToDoubleConverter : BooleanConverter<double>
    {
        public CustomBooleanToDoubleConverter() : base(1.0, 0.0) { }
    }
}
