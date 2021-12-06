using System;
using System.Globalization;
using System.Windows.Data;

namespace BowieD.MusicPlayer.WPF.Extensions
{
    public class DoubleToDisplayTimeConverter : IValueConverter
    {
        public static string ConvertSpan(TimeSpan span)
        {
            if (span.TotalHours >= 1)
            {
                return span.ToString("hh':'mm':'ss");
            }

            return span.ToString("mm':'ss");
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                TimeSpan span = TimeSpan.FromSeconds(doubleValue);

                return ConvertSpan(span);
            }

            throw new InvalidCastException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class DoubleToReadableDisplayTimeConverter : IValueConverter
    {
        public static string ConvertSpan(TimeSpan span)
        {
            if (span.TotalHours >= 1)
            {
                return span.ToString("hh' hrs. 'mm' mins. 'ss' secs.'");
            }

            return span.ToString("mm' mins. 'ss' secs.'");
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                TimeSpan span = TimeSpan.FromSeconds(doubleValue);

                return ConvertSpan(span);
            }

            throw new InvalidCastException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
