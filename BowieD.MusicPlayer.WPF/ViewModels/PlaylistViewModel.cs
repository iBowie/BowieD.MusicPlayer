using BowieD.MusicPlayer.WPF.Data;
using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.Views;
using GongSolutions.Wpf.DragDrop;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace BowieD.MusicPlayer.WPF.ViewModels
{
    public sealed class PlaylistViewModel : BaseViewModelView<MainWindow>
    {
        private readonly ObservableCollection<Song> _songs = new();
        private PlaylistInfo _playlistInfo;
        private Playlist _playlist;
        private readonly PlaylistViewModelDropHandler _dropHandler;

        public PlaylistViewModel(MainWindow view) : base(view)
        {
            _dropHandler = new PlaylistViewModelDropHandler(this);
        }

        private void _songs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            PlaylistInfo.SongIDs.Clear();

            foreach (var s in _songs)
                PlaylistInfo.SongIDs.Add(s.ID);

            PlaylistRepository.Instance.UpdatePlaylist(PlaylistInfo);
        }

        public PlaylistInfo PlaylistInfo
        {
            get => _playlistInfo;
            set
            {
                _songs.CollectionChanged -= _songs_CollectionChanged;

                _playlist = (Playlist)value;

                _songs.Clear();

                foreach (var s in _playlist.Songs)
                    _songs.Add(s);

                if (!value.IsEmpty)
                {
                    _songs.CollectionChanged += _songs_CollectionChanged;
                }

                ChangeProperty(ref _playlistInfo, value, nameof(PlaylistInfo), nameof(Playlist), nameof(Songs));
            }
        }
        public Playlist Playlist
        {
            get => _playlist;
        }
        public PlaylistViewModelDropHandler DropHandler => _dropHandler;

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
                        EditPlaylistDetailsView detailsView = new(PlaylistInfo)
                        {
                            Owner = View
                        };

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
    public sealed class PlaylistViewModelDropHandler : IDropTarget
    {
        private readonly PlaylistViewModel viewModel;

        public PlaylistViewModelDropHandler(PlaylistViewModel viewModel)
        {
            this.viewModel = viewModel;
        }

        public void DragOver(IDropInfo dropInfo)
        {
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;

            var dataObject = dropInfo.Data as IDataObject;

            if (dataObject is not null && dataObject.GetDataPresent(DataFormats.FileDrop))
            {
                dropInfo.Effects = DragDropEffects.Copy;
            }
            else
            {
                dropInfo.Effects = DragDropEffects.Move;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            var dataObject = dropInfo.Data as DataObject;

            if (dataObject is not null && dataObject.ContainsFileDropList())
            {
                var files = dataObject.GetFileDropList();

                if (files.Count > 0)
                {
                    var info = viewModel.PlaylistInfo;

                    foreach (var fn in files)
                    {
                        if (string.IsNullOrWhiteSpace(fn))
                            continue;

                        var song = SongRepository.Instance.GetOrAddSong(fn);

                        info.SongIDs.Add(song.ID);
                    }

                    PlaylistRepository.Instance.UpdatePlaylist(info);

                    viewModel.View.ViewModel.ObtainPlaylists();

                    viewModel.View.ViewModel.SelectedPlaylist = info;
                }
            }
            else
            {
                GongSolutions.Wpf.DragDrop.DragDrop.DefaultDropHandler.Drop(dropInfo);
            }
        }
    }
}
