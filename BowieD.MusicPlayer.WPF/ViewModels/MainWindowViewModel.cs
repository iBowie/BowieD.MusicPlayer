using BowieD.MusicPlayer.WPF.Common;
using BowieD.MusicPlayer.WPF.Data;
using BowieD.MusicPlayer.WPF.Extensions;
using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.Views;
using BowieD.MusicPlayer.WPF.Views.Pages;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BowieD.MusicPlayer.WPF.ViewModels
{
    public sealed class MainWindowViewModel : BaseViewModelView<MainWindow>
    {
        private static readonly Duration BACKGROUND_FADE_SPEED = new(TimeSpan.FromSeconds(1));
        private static readonly DoubleAnimation BACKGROUND_FADE_OUT = new(1.0, 0.0, BACKGROUND_FADE_SPEED);
        private static readonly DoubleAnimation BACKGROUND_FADE_IN = new(0.0, 1.0, BACKGROUND_FADE_SPEED);
        private Image bg1, bg2;
        private DispatcherTimer _fullScreenBackgroundSwitcher;
        private string? prevRandom;

        public MainWindowViewModel(MainWindow view) : base(view)
        {
            bg1 = View.fullScreenBackground;
            bg2 = View.fullScreenBackground2;

            _fullScreenBackgroundSwitcher = new()
            {
                Interval = TimeSpan.FromSeconds(60)
            };

            _fullScreenBackgroundSwitcher.Tick += _fullScreenBackgroundSwitcher_Tick;

            ObtainPlaylists();
        }

        private void _fullScreenBackgroundSwitcher_Tick(object? sender, EventArgs? e)
        {
            if (!View.MusicPlayerViewModel.IsFullScreen)
                return;

            if (Backgrounds.Count > 1)
            {
                var fileName = Backgrounds.Where(d => d != prevRandom).ToList().Random();

                SetBackgroundFromFile(fileName);
            }
        }

        private void SetBackgroundFromFile(string fileName)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(fileName);
            bmp.EndInit();

            bg2.Source = bmp;

            bg1.BeginAnimation(Image.OpacityProperty, BACKGROUND_FADE_OUT);
            bg2.BeginAnimation(Image.OpacityProperty, BACKGROUND_FADE_IN);

            var t = bg1;
            bg1 = bg2;
            bg2 = t;

            prevRandom = fileName;
        }

        #region Commands

        private ICommand? _addSongCommand, _createPlaylistCommand, _selectFullscreenBackgroundCommand,
            _openLibraryCommand;

        public ICommand AddSongCommand
        {
            get
            {
                if (_addSongCommand is null)
                {
                    _addSongCommand = new BaseCommand(() =>
                    {
                        OpenFileDialog ofd = new();

                        if (ofd.ShowDialog() == true)
                        {
                            SongRepository.Instance.GetOrAddSong(ofd.FileName);
                        }
                    });
                }

                return _addSongCommand;
            }
        }
        public ICommand CreatePlaylistCommand
        {
            get
            {
                if (_createPlaylistCommand is null)
                {
                    _createPlaylistCommand = new BaseCommand(() =>
                    {
                        PlaylistInfo newPlaylist = new(0, "Playlist 1", Array.Empty<long>(), Array.Empty<byte>());

                        PlaylistRepository.Instance.AddNewPlaylist(ref newPlaylist);

                        ObtainPlaylists();
                    });
                }

                return _createPlaylistCommand;
            }
        }
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
        public ICommand OpenLibraryCommand
        {
            get
            {
                if (_openLibraryCommand is null)
                {
                    _openLibraryCommand = new BaseCommand(() =>
                    {
                        LibraryPage page = new(new(View));

                        View.navFrame.Navigate(page);
                    });
                }

                return _openLibraryCommand;
            }
        }

        #endregion

        #region Data

        private readonly ObservableCollection<string> _backgrounds = new();
        private readonly ObservableCollection<PlaylistInfo> _playlists = new();
        private double _blurPower = 20.0;

        #endregion

        #region Properties

        public ObservableCollection<PlaylistInfo> Playlists
        {
            get => _playlists;
        }

        public ObservableCollection<string> Backgrounds
        {
            get => _backgrounds;
        }

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

        public string WindowTitle
        {
            get
            {
                var song = View.MusicPlayerViewModel.CurrentSong;

                if (song.IsEmpty || BassFacade.State != Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING)
                {
                    return "BDMP";
                }
                else
                {
                    return $"{song.Artist} - {song.Title} | BDMP";
                }
            }
        }

        public PlaylistInfo SelectedPlaylist
        {
            get { return (PlaylistInfo)GetValue(SelectedPlaylistProperty); }
            set { SetValue(SelectedPlaylistProperty, value); }
        }

        public static readonly DependencyProperty SelectedPlaylistProperty = DependencyProperty.Register("SelectedPlaylist", typeof(PlaylistInfo), typeof(MainWindowViewModel), new PropertyMetadata(PlaylistInfo.EMPTY, SelectedPlaylistChangedCallback));

        private static void SelectedPlaylistChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is MainWindowViewModel viewModel && e.NewValue is PlaylistInfo pinfo)
            {
                var page = new PlaylistPage(new(pinfo, viewModel.View));

                viewModel.View.navFrame.Navigate(page);
            }
        }

        #endregion

        public void ObtainPlaylists()
        {
            _playlists.Clear();

            System.Collections.Generic.IList<PlaylistInfo> pls = PlaylistRepository.Instance.GetAllPlaylists();

            foreach (var pl in pls)
            {
                _playlists.Add(pl);
            }
        }

        public void SetBackground(params string[] fileNames)
        {
            Backgrounds.Clear();

            if (fileNames.Length == 0)
            {
                _fullScreenBackgroundSwitcher.Stop();

                View.fullScreenBackground.Source = null;
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
    }
}
