using BowieD.MusicPlayer.WPF.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace BowieD.MusicPlayer.WPF.Common
{
    public static class CoverAnalyzer
    {
        public static byte[] GenerateCoverArt(this IEnumerable<Song> songs, bool pickOnlyOne = false)
        {
            if (!songs.Any())
                return Array.Empty<byte>();

            const int resX = 640, resY = 640;

            Image resultImage = new Bitmap(resX, resY);

            using (Graphics g = Graphics.FromImage(resultImage))
            {
                var validPictures = songs.Select(d => d.PictureData).Where(d => d is not null && d.Length > 0).Distinct().Take(pickOnlyOne ? 1 : 4).ToArray();

                if (validPictures.Length >= 4)
                {
                    const int cX = 2, cY = 2;
                    const int chunkX = resX / cX;
                    const int chunkY = resY / cY;

                    for (int i = 0; i < cX; i++)
                    {
                        for (int j = 0; j < cY; j++)
                        {
                            int index = i + j * cX;

                            var picture = validPictures[index];

                            using MemoryStream ms = new(picture);

                            var bmp = Bitmap.FromStream(ms);

                            g.DrawImage(bmp, chunkX * i, chunkY * j, chunkX, chunkY);
                        }
                    }
                }
                else if (validPictures.Length >= 1)
                {
                    var picture = validPictures.First();

                    using MemoryStream ms = new(picture);

                    var bmp = Bitmap.FromStream(ms);

                    g.DrawImage(bmp, 0, 0, resX, resY);
                }
                else
                {
                    return Array.Empty<byte>();
                }
            }

            byte[] res;

            using (MemoryStream ms = new())
            {
                resultImage.Save(ms, ImageFormat.Jpeg);

                res = ms.ToArray();
            }

            return res;
        }
    }
}
