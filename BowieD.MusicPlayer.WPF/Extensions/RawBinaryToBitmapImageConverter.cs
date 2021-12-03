using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace BowieD.MusicPlayer.WPF.Extensions
{
    public class RawBinaryToBitmapImageConverter : IValueConverter
    {
        private static readonly Dictionary<string, BitmapImage> _cache = new();

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is byte[] rawData && rawData.Length > 0)
            {
                var hash = GetHash(rawData);

                if (!_cache.ContainsKey(hash))
                {
                    BitmapImage bmp = new();

                    using (MemoryStream ms = new(rawData))
                    {
                        bmp.BeginInit();
                        bmp.StreamSource = ms;
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                    }

                    bmp.Freeze();

                    _cache[hash] = bmp;
                }

                return _cache[hash];
            }

            return null;
        }

        private string GetHash(byte[] array)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(array);

                return System.Convert.ToHexString(hash);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
