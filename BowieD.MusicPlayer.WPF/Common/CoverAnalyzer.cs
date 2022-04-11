using BowieD.MusicPlayer.WPF.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;

namespace BowieD.MusicPlayer.WPF.Common
{
    public static class CoverAnalyzer
    {
        private sealed class FuzzyImageComparer : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[]? x, byte[]? y)
            {
                if (x is null && y is null)
                    return true;

                if (x is null || y is null)
                    return false;

                if (x == y)
                    return true;

                if (x.SequenceEqual(y))
                    return true;

                using var xMs = new MemoryStream(x);
                using var yMs = new MemoryStream(y);
                using var xImg = new Bitmap(xMs);
                using var yImg = new Bitmap(yMs);

                string getHash(Bitmap bitmap)
                {
                    var lowRes = new Bitmap(bitmap, 16, 16);

                    List<bool> bits = new();

                    for (int i = 0; i < 16; i++)
                    {
                        for (int j = 0; j < 16; j++)
                        {
                            var pixel = lowRes.GetPixel(i, j);

                            bits.Add(pixel.GetBrightness() > 0.5);
                        }
                    }

                    var binary = convertToBinary(bits);

                    return string.Join("", binary.Select(d => d.ToString("X")));
                }

                byte[] convertToBinary(IList<bool> bits)
                {
                    byte[] res = new byte[(int)Math.Ceiling(bits.Count / 8.0)];

                    for (int i = 0; i < res.Length; i++)
                    {
                        byte num = 0x0;

                        for (int j = 0; j < 8; j++)
                        {
                            var bit = bits[i + j];

                            if (bit)
                            {
                                num |= (byte)(1 << j);
                            }
                        }
                    }

                    return res;
                }

                return getHash(xImg) == getHash(yImg);
            }

            public int GetHashCode([DisallowNull] byte[] obj)
            {
                return 1;
            }
        }
        private static readonly FuzzyImageComparer _comparer = new();

        public static byte[] GenerateCoverArt(this IEnumerable<Song> songs, bool pickOnlyOne = false)
        {
            if (!songs.Any())
                return Array.Empty<byte>();

            const int resX = 640, resY = 640;

            Image resultImage = new Bitmap(resX, resY);

            using (Graphics g = Graphics.FromImage(resultImage))
            {
                var validPictures = songs.Select(d => d.PictureData).Where(d => d is not null && d.Length > 0).Distinct(_comparer).Take(pickOnlyOne ? 1 : 4).ToArray();

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
