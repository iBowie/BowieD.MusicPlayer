using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
}
