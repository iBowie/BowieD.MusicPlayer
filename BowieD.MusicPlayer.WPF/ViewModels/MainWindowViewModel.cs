using BowieD.MusicPlayer.WPF.Data;
using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.Views;
using BowieD.MusicPlayer.WPF.Views.Pages;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace BowieD.MusicPlayer.WPF.ViewModels
{
    public sealed class MainWindowViewModel : BaseViewModelView<MainWindow>
    {
        public MainWindowViewModel(MainWindow view) : base(view)
        {
            ObtainPlaylists();
        }

        #region Commands

        private ICommand? _addSongCommand, _createPlaylistCommand, _openLibraryCommand;
        private ICommand? _showWindowCommand, _hideWindowCommand, _exitCommand;
        private ICommand? _openVisualizerCommand;

        private VisualizerWindow? _visualizerWindow;

        public ICommand OpenVisualizerCommand
        {
            get
            {
                return _openVisualizerCommand ??= new BaseCommand(() =>
                {
                    _visualizerWindow = new VisualizerWindow(View.MusicPlayerViewModel);

                    _visualizerWindow.Show();

                    _visualizerWindow.Closed += (sender, e) =>
                    {
                        _visualizerWindow = null;
                    };
                }, () =>
                {
                    return _visualizerWindow is null;
                });
            }
        }
        public ICommand ShowWindowCommand
        {
            get
            {
                return _showWindowCommand ??= new BaseCommand(() =>
                {
                    View.Show();
                });
            }
        }
        public ICommand HideWindowCommand
        {
            get
            {
                return _hideWindowCommand ??= new BaseCommand(() =>
                {
                    View.Hide();
                });
            }
        }
        public ICommand ExitCommand
        {
            get
            {
                return _exitCommand ??= new BaseCommand(() =>
                {
                    View.CloseCompletely();
                });
            }
        }
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
                        PlaylistInfo newPlaylist = new(0, "Playlist 1", Array.Empty<string>(), Array.Empty<byte>());

                        PlaylistRepository.Instance.AddNewPlaylist(ref newPlaylist);

                        ObtainPlaylists();
                    });
                }

                return _createPlaylistCommand;
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
        private ICommand? _openAllSongsCommand;
        public ICommand OpenAllSongsCommand
        {
            get
            {
                if (_openAllSongsCommand is null)
                {
                    _openAllSongsCommand = new BaseCommand(() =>
                    {
                        AllSongsPage page = new(View);

                        View.navFrame.Navigate(page);
                    });
                }

                return _openAllSongsCommand;
            }
        }
        private ICommand? _openSettingsCommand;
        public ICommand OpenSettingsCommand
        {
            get
            {
                if (_openSettingsCommand is null)
                {
                    _openSettingsCommand = new BaseCommand(() =>
                    {
                        SettingsPage page = new(View);

                        View.navFrame.Navigate(page);
                    });
                }

                return _openSettingsCommand;
            }
        }

        #endregion

        #region Properties
        public ObservableCollection<PlaylistInfo> Playlists { get; } = new();

        public string WindowTitle
        {
            get
            {
                var song = View.MusicPlayerViewModel.CurrentSong;

                if (song.IsEmpty || View.MusicPlayerViewModel.BassWrapper.State != Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING)
                {
                    return "BDMP";
                }
                else
                {
                    string displayArtist, displayTitle;

                    if (string.IsNullOrWhiteSpace(song.Artist))
                    {
                        displayArtist = "Unknown";
                    }
                    else
                    {
                        displayArtist = song.Artist;
                    }

                    if (string.IsNullOrWhiteSpace(song.Title))
                    {
                        displayTitle = "Unnamed";
                    }
                    else
                    {
                        displayTitle = song.Title;
                    }

                    return $"{displayArtist} - {displayTitle} | BDMP";
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
            Playlists.Clear();

            System.Collections.Generic.IList<PlaylistInfo> pls = PlaylistRepository.Instance.GetAllPlaylists();

            foreach (var pl in pls)
            {
                Playlists.Add(pl);
            }
        }
    }
}
