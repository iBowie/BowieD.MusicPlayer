using System;
using System.Globalization;
using System.Windows.Data;

namespace BowieD.MusicPlayer.WPF.Extensions
{
    public sealed class ArtistDisplayConverter : IValueConverter
    {
        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            var res = value?.ToString()?
                             .Replace(";", ", ")
                             .Replace("/", ", ")
                             ?? null;

            if (parameter is IValueConverter subConverter)
            {
                return subConverter.Convert(res, targetType, null, culture);
            }

            return res;
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }
}
