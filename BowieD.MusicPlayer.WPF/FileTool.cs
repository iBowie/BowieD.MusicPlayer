using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace BowieD.MusicPlayer.WPF
{
    public static class FileTool
    {
        public static bool CheckFileValid([NotNullWhen(true)] string? fileName, IEnumerable<string> supportedExtensions)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            if (!File.Exists(fileName))
                return false;

            if (!supportedExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase))
                return false;

            return true;
        }

        public static double GetDuration(string fileName)
        {
            MediaInfo.MediaInfoWrapper miw = new(fileName);


            return miw.Duration / 1000.0;
        }
    }
    public static class ImageTool
    {
        private static readonly string[] SUPPORTED_EXTENSIONS = new string[]
        {
            ".png",
            ".jpeg",
            ".jpg",
            ".bmp"
        };

        public static IEnumerable<string> SupportedImageExtensions = SUPPORTED_EXTENSIONS;

        public static byte[] ResizeInByteArray(byte[] imageBytes, int newMaxWidth, int newMaxHeight)
        {
            using MemoryStream fromStream = new(imageBytes);

            Bitmap bmp = new(fromStream);

            newMaxWidth = Math.Min(bmp.Width, newMaxWidth);
            newMaxHeight = Math.Min(bmp.Height, newMaxHeight);

            Bitmap result = new(bmp, newMaxWidth, newMaxHeight);

            using MemoryStream toStream = new();

            result.Save(toStream, ImageFormat.Jpeg);

            return toStream.ToArray();
        }
    }
}
