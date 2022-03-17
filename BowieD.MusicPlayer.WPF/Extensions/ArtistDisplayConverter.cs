using System;
using System.Globalization;
using System.Windows.Data;

namespace BowieD.MusicPlayer.WPF.Extensions
{
    public sealed class ArtistDisplayConverter : IValueConverter
    {
        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            return value?.ToString()?.Replace(";", ", ").Replace("/", ", ") ?? null;
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }
}
