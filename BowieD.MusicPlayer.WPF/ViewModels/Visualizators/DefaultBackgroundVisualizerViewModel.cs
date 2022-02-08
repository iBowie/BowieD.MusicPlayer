using BowieD.MusicPlayer.WPF.Data;
using BowieD.MusicPlayer.WPF.Extensions;
using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.MVVM;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BowieD.MusicPlayer.WPF.ViewModels.Visualizators
{
    public sealed class DefaultBackgroundVisualizerViewModel : VisualizerViewModelBase
    {
        private static readonly Duration BACKGROUND_FADE_SPEED = new(TimeSpan.FromSeconds(1));
        private static readonly DoubleAnimation BACKGROUND_FADE_OUT = new(1.0, 0.0, BACKGROUND_FADE_SPEED);
        private static readonly DoubleAnimation BACKGROUND_FADE_IN = new(0.0, 1.0, BACKGROUND_FADE_SPEED);
        private Image bg1, bg2;
        private readonly DispatcherTimer _fullScreenBackgroundSwitcher;
        private double _blurPower = 20.0;
        private string? prevRandom;

        public DefaultBackgroundVisualizerViewModel(Panel boundPanel, MainWindowViewModel mainWindowViewModel) : base(boundPanel, mainWindowViewModel)
        {
            _fullScreenBackgroundSwitcher = new()
            {
                Interval = TimeSpan.FromSeconds(60),
            };

            _fullScreenBackgroundSwitcher.Tick += _fullScreenBackgroundSwitcher_Tick;
            
            _fullScreenBackgroundSwitcher.Stop();
        }

        private void _fullScreenBackgroundSwitcher_Tick(object? sender, EventArgs e)
        {
            if (Backgrounds.Count > 1)
            {
                var fileName = Backgrounds.Where(d => d != prevRandom).ToList().Random();

                SetBackgroundFromFile(fileName);
            }
        }

        public override string VisualizerName => "Image Background";

        public ObservableCollection<string> Backgrounds { get; } = new();
        public double BlurPower
        {
            get => _blurPower;
            set => ChangeProperty(ref _blurPower, value, nameof(BlurPower));
        }
        public double BackgroundSwitchSpeedSeconds
        {
            get => _fullScreenBackgroundSwitcher.Interval.TotalSeconds;
            set => _fullScreenBackgroundSwitcher.Interval = TimeSpan.FromSeconds(value);
        }

        public override void Setup()
        {
            bg1 = MainWindowViewModel.View.fullScreenBackground;
            bg2 = MainWindowViewModel.View.fullScreenBackground2;
        }

        public override void Start()
        {
            _fullScreenBackgroundSwitcher.Start();

            MusicPlayerViewModel.OnTrackChanged += MusicPlayerViewModel_OnTrackChanged;
        }

        public override void Stop()
        {
            MusicPlayerViewModel.OnTrackChanged -= MusicPlayerViewModel_OnTrackChanged;

            _fullScreenBackgroundSwitcher.Stop();
        }

        private void MusicPlayerViewModel_OnTrackChanged(Song newSong)
        {
            if (Backgrounds.Count == 0)
            {
                SetBackgroundFromBytes(newSong.FullScreenPictureData);
            }
        }

        private void SetBackgroundFromFile(string fileName)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(fileName);
            bmp.EndInit();
            bmp.Freeze();

            bg2.Source = bmp;

            bg1.BeginAnimation(Image.OpacityProperty, BACKGROUND_FADE_OUT);
            bg2.BeginAnimation(Image.OpacityProperty, BACKGROUND_FADE_IN);

            var t = bg1;
            bg1 = bg2;
            bg2 = t;

            prevRandom = fileName;
        }

        private void SetBackgroundFromBytes(byte[] data)
        {
            ImageSource? src;

            if (data is null || data.Length == 0)
            {
                src = null;
            }
            else
            {
                using var ms = new MemoryStream(data);

                BitmapImage? bmp = new();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();

                src = bmp;
            }

            bg2.Source = src;

            bg1.BeginAnimation(Image.OpacityProperty, BACKGROUND_FADE_OUT);
            bg2.BeginAnimation(Image.OpacityProperty, BACKGROUND_FADE_IN);

            var t = bg1;
            bg1 = bg2;
            bg2 = t;
        }

        public void SetBackground(params string[] fileNames)
        {
            Backgrounds.Clear();

            if (fileNames.Length == 0)
            {
                _fullScreenBackgroundSwitcher.Stop();

                MainWindowViewModel.View.fullScreenBackground.Source = null;
            }
            else if (fileNames.Length == 1)
            {
                _fullScreenBackgroundSwitcher.Stop();

                Backgrounds.Add(fileNames[0]);
                SetBackgroundFromFile(fileNames[0]);
            }
            else
            {
                foreach (var fn in fileNames)
                {
                    Backgrounds.Add(fn);
                }

                SetBackgroundFromFile(Backgrounds.Random());

                _fullScreenBackgroundSwitcher.Start();
            }
        }
        private ICommand? _selectFullscreenBackgroundCommand;
        public ICommand SelectFullscreenBackgroundCommand
        {
            get
            {
                if (_selectFullscreenBackgroundCommand is null)
                {
                    _selectFullscreenBackgroundCommand = new BaseCommand(() =>
                    {
                        OpenFileDialog ofd = new()
                        {
                            Filter = ImageTool.FileDialogFilter,
                            CheckFileExists = true,
                            Multiselect = true
                        };

                        if (ofd.ShowDialog() == true)
                        {
                            try
                            {
                                SetBackground(ofd.FileNames);
                            }
                            catch { }
                        }
                    });
                }

                return _selectFullscreenBackgroundCommand;
            }
        }
        private ICommand? _setSongBackgroundCommand;
        public ICommand SetSongBackgroundImageCommand
        {
            get
            {
                if (_setSongBackgroundCommand is null)
                {
                    _setSongBackgroundCommand = new BaseCommand(() =>
                    {
                        OpenFileDialog ofd = new()
                        {
                            Filter = ImageTool.FileDialogFilter,
                            CheckFileExists = true,
                            Multiselect = false
                        };

                        if (ofd.ShowDialog() == true)
                        {
                            try
                            {
                                byte[] raw = File.ReadAllBytes(ofd.FileName);
                                Backgrounds.Clear();
                                SetBackgroundFromBytes(raw);
                                var current = MainWindowViewModel.View.MusicPlayerViewModel.CurrentSong;
                                current.FullScreenPictureData = raw;
                                SongRepository.Instance.UpdateSong(current, false);

                                foreach (var aas in MainWindowViewModel.View.MusicPlayerViewModel.AllActiveSongs)
                                {
                                    if (aas.ID == current.ID)
                                    {
                                        aas.UpdateFromDatabase();
                                    }
                                }

                                MainWindowViewModel.View.MusicPlayerViewModel.SongHistory.RaiseChanged();
                                MainWindowViewModel.View.MusicPlayerViewModel.SongQueue.RaiseChanged();
                                MainWindowViewModel.View.MusicPlayerViewModel.UserSongQueue.RaiseChanged();
                            }
                            catch { }
                        }
                    }, () =>
                    {
                        return !MainWindowViewModel.View.MusicPlayerViewModel.CurrentSong.IsEmpty;
                    });
                }

                return _setSongBackgroundCommand;
            }
        }
    }
}
