using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace BowieD.MusicPlayer.WPF.Common
{
    public static class VibrantColor
    {
        public static System.Windows.Media.Color GetVibrantColor(byte[] pictureData)
        {
            if (pictureData.Length == 0)
                return System.Windows.Media.Colors.White;

            using MemoryStream ms = new(pictureData);

            using Bitmap orig = new(ms);

            return GetVibrantColor(orig);
        }

        public static System.Windows.Media.Color GetVibrantColor(Bitmap bmp)
        {
            using Bitmap thumb = new(bmp, new Size(128, 128));

            Dictionary<Color, int> colors = new();

            for (int x = 0; x < thumb.Width; x++)
            {
                for (int y = 0; y < thumb.Height; y++)
                {
                    var pixel = thumb.GetPixel(x, y);

                    if (colors.ContainsKey(pixel))
                    {
                        colors[pixel] += 1;
                    }
                    else
                    {
                        colors[pixel] = 1;
                    }
                }
            }

            var orderedColors = colors
                .OrderByDescending(d => d.Value)
                .Select(d => d.Key)
                .Where(d => d.A == byte.MaxValue && d.GetBrightness() < 0.75 && d.GetBrightness() > 0.25)
                .OrderByDescending(d => d.GetSaturation());

            System.Drawing.Color selectedColor;

            if (orderedColors.Any())
            {
                selectedColor = orderedColors.First();
            }
            else
            {
                selectedColor = colors.First().Key;
            }

            return System.Windows.Media.Color.FromArgb(selectedColor.A, selectedColor.R, selectedColor.G, selectedColor.B);
        }
    }
}
