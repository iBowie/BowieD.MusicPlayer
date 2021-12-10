using System;
using System.IO;

namespace BowieD.MusicPlayer.WPF
{
    public static class DataFolder
    {
        private static string? _dataDirectory;

        public static string DataDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(_dataDirectory))
                {
                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);

                    _dataDirectory = Path.Combine(appData, "BowieD", "MusicPlayer");

                    if (!Directory.Exists(_dataDirectory))
                        Directory.CreateDirectory(_dataDirectory);
                }

                return _dataDirectory;
            }
        }
    }
}
