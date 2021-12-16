using MahApps.Metro.Controls;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BowieD.MusicPlayer.WPF.Views
{
    /// <summary>
    /// Логика взаимодействия для YouTubeDownloadView.xaml
    /// </summary>
    public partial class YouTubeDownloadView : MetroWindow
    {
        public readonly string videoId;
        public readonly string fileNameNoExtension;
        private Process? process;
        private bool isSuccess = false;

        public YouTubeDownloadView(string fileNameNoExtension, string videoId)
        {
            InitializeComponent();

            this.videoId = videoId;
            this.fileNameNoExtension = fileNameNoExtension;

            Loaded += (sender, e) =>
            {
                if (string.IsNullOrWhiteSpace(videoId))
                    throw new Exception();

                string url = $"https://www.youtube.com/watch?v={videoId}";

                ProcessStartInfo psi = new("yt-dlp.exe", $"-x --audio-format mp3 --embed-thumbnail --add-metadata -o \"{fileNameNoExtension}.%(ext)s\" {url}")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                };

                process = Process.Start(psi);

                if (process is null || process.HasExited)
                    throw new Exception();

                process.EnableRaisingEvents = true;
                process.BeginOutputReadLine();
                process.OutputDataReceived += P_OutputDataReceived;
                process.Exited += P_Exited;

                try
                {
                    process.WaitForInputIdle();
                }
                catch { }
            };

            Closed += (sender, e) =>
            {
                if (process is not null && !process.HasExited && DialogResult != true)
                {
                    process.Kill();
                    DialogResult = false;
                }
            };
        }

        private void P_Exited(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                DialogResult = isSuccess;
                Close();
            });
        }

        private void P_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
                return;

            Debug.WriteLine(e.Data, "YouTube-DL");

            var m1 = Regex.Match(e.Data, @"\[(youtube|download)\]");

            var outType = m1.Groups[1].Value;

            Dispatcher.Invoke(() =>
            {
                switch (outType.ToLowerInvariant())
                {
                    case "youtube": // prepare
                        {
                            progBar.Visibility = System.Windows.Visibility.Collapsed;
                        }
                        break;

                    case "download": // downloading progress
                        {
                            var m3 = Regex.Match(e.Data, @"\[download\] 100\%");

                            if (m3.Success)
                            {
                                progBar.Visibility = System.Windows.Visibility.Collapsed;
                                statusText.Text = "Downloaded. Converting...";
                                isSuccess = true;
                            }
                            else
                            {
                                var m2 = Regex.Match(e.Data, @"\[download\] +([0-9]+\.[0-9]+)\%(?:.+)ETA ([0-9]+(?:\:[0-9]+){1,})");

                                // group 1
                                // progress
                                // group 2
                                // ETA

                                if (m2.Success)
                                {
                                    progBar.Visibility = System.Windows.Visibility.Visible;

                                    statusText.Text = $"Downloading video '{videoId}'\nProgress: {m2.Groups[1].Value}\nETA: {m2.Groups[2].Value}";
                                    progBar.Value = double.Parse(m2.Groups[1].Value, CultureInfo.InvariantCulture);
                                }
                            }
                        }
                        break;
                }
            });
        }
    }
}
