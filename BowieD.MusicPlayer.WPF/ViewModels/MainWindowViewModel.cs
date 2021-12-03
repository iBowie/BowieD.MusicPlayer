using BowieD.MusicPlayer.WPF.Common;
using BowieD.MusicPlayer.WPF.Data;
using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.Views;
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

        private ICommand _addSongCommand, _createPlaylistCommand;

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

        #endregion

        #region Data

        private readonly ObservableCollection<PlaylistInfo> _playlists = new();

        #endregion

        #region Properties

        public ObservableCollection<PlaylistInfo> Playlists
        {
            get => _playlists;
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
                viewModel.View.PlaylistViewModel.PlaylistInfo = pinfo;
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
    }
}
