using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace BowieD.MusicPlayer.WPF.Extensions
{
    public class RawBinaryToBitmapImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is byte[] rawData && rawData.Length > 0)
            {
                int decodeW = 0, decodeH = 0;

                if (parameter is not null)
                {
                    string pString = parameter.ToString() ?? string.Empty;

                    string[] spl = pString.Split('|');

                    if (spl.Length == 2 && int.TryParse(spl[0], out var newW) &&
                        int.TryParse(spl[1], out var newH))
                    {
                        decodeW = Math.Max(decodeW, newW);
                        decodeH = Math.Max(decodeH, newH);
                    }
                }

                BitmapImage bmp = new();

                using (MemoryStream ms = new(rawData))
                {
                    bmp.BeginInit();
                    bmp.StreamSource = ms;
                    bmp.CacheOption = BitmapCacheOption.OnLoad;

                    if (decodeH > 0)
                        bmp.DecodePixelHeight = decodeH;

                    if (decodeW > 0)
                        bmp.DecodePixelWidth = decodeW;

                    bmp.EndInit();
                }

                bmp.Freeze();

                return bmp;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
