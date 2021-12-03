using BowieD.MusicPlayer.WPF.Data;
using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace BowieD.MusicPlayer.WPF.ViewModels
{
    public sealed class PlaylistViewModel : BaseViewModelView<MainWindow>
    {
        private readonly ObservableCollection<Song> _songs = new();
        private PlaylistInfo _playlistInfo;
        private Playlist _playlist;

        public PlaylistViewModel(MainWindow view) : base(view) { }

        public PlaylistInfo PlaylistInfo
        {
            get => _playlistInfo;
            set
            {
                _playlist = (Playlist)value;

                _songs.Clear();

                foreach (var s in _playlist.Songs)
                    _songs.Add(s);

                ChangeProperty(ref _playlistInfo, value, nameof(PlaylistInfo), nameof(Playlist), nameof(Songs));
            }
        }
        public Playlist Playlist
        {
            get => _playlist;
        }

        public ObservableCollection<Song> Songs
        {
            get => _songs;
        }

        private ICommand _editDetailsCommand, _playCommand;

        public ICommand EditDetailsCommand
        {
            get
            {
                if (_editDetailsCommand is null)
                {
                    _editDetailsCommand = new BaseCommand(() =>
                    {
                        EditPlaylistDetailsView detailsView = new(PlaylistInfo);

                        if (detailsView.ShowDialog() == true)
                        {
                            PlaylistInfo result = detailsView.ResultInfo;
                            
                            PlaylistRepository.Instance.UpdatePlaylist(result);

                            View.ViewModel.ObtainPlaylists();

                            View.ViewModel.SelectedPlaylist = result;
                        }
                    });
                }

                return _editDetailsCommand;
            }
        }
        public ICommand PlayCommand
        {
            get
            {
                if (_playCommand is null)
                {
                    _playCommand = new BaseCommand(() =>
                    {
                        View.MusicPlayerViewModel.PlayPlaylist(Playlist, true);
                    });
                }

                return _playCommand;
            }
        }
    }
}
