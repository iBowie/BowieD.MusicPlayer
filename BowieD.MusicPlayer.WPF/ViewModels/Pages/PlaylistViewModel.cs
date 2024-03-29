﻿using BowieD.MusicPlayer.WPF.Common;
using BowieD.MusicPlayer.WPF.Data;
using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.Views;
using BowieD.MusicPlayer.WPF.Views.Pages;
using GongSolutions.Wpf.DragDrop;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace BowieD.MusicPlayer.WPF.ViewModels.Pages
{
    public sealed class PlaylistViewModel : BaseViewModelView<MainWindow>
    {
        private readonly ObservableCollection<Song> _songs = new();
        private Playlist _playlist;
        private readonly PlaylistViewModelDropHandler _dropHandler;

        public PlaylistViewModel(PlaylistPage page, MainWindow view) : base(view)
        {
            Page = page;

            _dropHandler = new PlaylistViewModelDropHandler(this);
        }

        public PlaylistPage Page { get; }

        private void _songs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Playlist.SongFileNames.Clear();

            foreach (var s in _songs)
                Playlist.SongFileNames.Add(s.FileName);

            PlaylistRepository.Instance.UpdatePlaylist(Playlist);

            View.ViewModel.ObtainPlaylists();
        }

        public Playlist Playlist
        {
            get => _playlist;
            set
            {
                _songs.CollectionChanged -= _songs_CollectionChanged;

                ChangeProperty(ref _playlist, value, nameof(Playlist));

                _songs.Clear();

                foreach (var s in _playlist.Songs)
                    _songs.Add(s);

                if (!value.IsEmpty)
                {
                    _songs.CollectionChanged += _songs_CollectionChanged;
                }

                TriggerPropertyChanged(nameof(Songs));
            }
        }
        public PlaylistViewModelDropHandler DropHandler => _dropHandler;

        public ObservableCollection<Song> Songs
        {
            get => _songs;
        }

        private ICommand? _editDetailsCommand, _playCommand, _editSongDetailsCommand,
            _addSongsToQueueCommand;

        public ICommand EditDetailsCommand
        {
            get
            {
                if (_editDetailsCommand is null)
                {
                    _editDetailsCommand = new BaseCommand(() =>
                    {
                        EditPlaylistDetailsView detailsView = new(Playlist)
                        {
                            Owner = View
                        };

                        if (detailsView.ShowDialog() == true)
                        {
                            Playlist result = detailsView.ResultInfo;

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
                        View.MusicPlayerViewModel.PlaySource(Playlist, true);
                    });
                }

                return _playCommand;
            }
        }
        public ICommand EditSongDetailsCommand
        {
            get
            {
                if (_editSongDetailsCommand is null)
                {
                    _editSongDetailsCommand = new GenericCommand<Song?>((p) =>
                    {
                        if (!p.HasValue)
                            return;

                        var song = p.Value;

                        var index = Songs.IndexOf(song);

                        if (index == -1)
                            return;

                        var mp = View.MusicPlayerViewModel;

                        EditSongDetailsView esdv = new(song);

                        esdv.LockState(mp);

                        if (esdv.ShowDialog() == true)
                        {
                            Song resultSong = esdv.ResultSong;

                            SongRepository.Instance.UpdateSong(resultSong, false);

                            Songs[index] = resultSong;

                            foreach (var aas in View.MusicPlayerViewModel.AllActiveSongs)
                            {
                                if (aas.ID == resultSong.ID)
                                {
                                    aas.UpdateFromDatabase();
                                }
                            }

                            View.MusicPlayerViewModel.SongHistory.RaiseChanged();
                            View.MusicPlayerViewModel.SongQueue.RaiseChanged();
                            View.MusicPlayerViewModel.UserSongQueue.RaiseChanged();
                        }

                        esdv.RestoreState(mp);
                    }, (p) =>
                    {
                        return p.HasValue && !p.Value.IsEmpty && p.Value.IsAvailable;
                    });
                }

                return _editSongDetailsCommand;
            }
        }
        public ICommand AddSongsToQueueCommand
        {
            get
            {
                if (_addSongsToQueueCommand is null)
                {
                    _addSongsToQueueCommand = new GenericCommand<IEnumerable>((songs) =>
                    {
                        foreach (var s in songs)
                        {
                            if (s is Song song)
                            {
                                if (song.IsEmpty || !song.IsAvailable)
                                    continue;

                                View.MusicPlayerViewModel.UserSongQueue.EnqueueFIFO(song);
                            }
                        }
                    }, (songs) =>
                    {
                        return songs is not null;
                    });
                }

                return _addSongsToQueueCommand;
            }
        }
    }
    public sealed class PlaylistViewModelDropHandler : IDropTarget
    {
        private const string REGEX_YT_PATTERN = @"http(?:s|)\:\/\/(?:(?:www|music)\.youtube\.com\/watch\?v=([a-zA-Z0-9\-_]+)|youtu\.be\/([a-zA-Z0-9\-_]+))";
        private readonly PlaylistViewModel viewModel;

        public PlaylistViewModelDropHandler(PlaylistViewModel viewModel)
        {
            this.viewModel = viewModel;
        }

        public void DragOver(IDropInfo dropInfo)
        {
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;

            var dataObject = dropInfo.Data as IDataObject;

            if (dataObject is not null &&
                (dataObject.GetDataPresent(DataFormats.FileDrop) || dataObject.GetDataPresent(DataFormats.Text)))
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

            if (dataObject is not null)
            {
                if (dataObject.ContainsFileDropList())
                {
                    var files = dataObject.GetFileDropList();

                    if (files.Count > 0)
                    {
                        var info = viewModel.Playlist;
                        int insertIndex = dropInfo.InsertIndex;

                        foreach (var fn in files)
                        {
                            if (!FileTool.CheckFileValid(fn, BassWrapper.SupportedExtensions))
                                continue;

                            var song = SongRepository.Instance.GetOrAddSong(fn);

                            info.SongFileNames.Insert(insertIndex++, song.FileName);
                        }

                        PlaylistRepository.Instance.UpdatePlaylist(info);

                        viewModel.Playlist = info;
                    }

                    return;
                }
                else if (dataObject.ContainsText(TextDataFormat.Text))
                {
                    string text = dataObject.GetText(TextDataFormat.Text);

                    var m = Regex.Match(text, REGEX_YT_PATTERN);

                    Group? rGroup;

                    if (m.Groups[1].Success)
                    {
                        rGroup = m.Groups[1];
                    }
                    else if (m.Groups[2].Success)
                    {
                        rGroup = m.Groups[2];
                    }
                    else
                    {
                        rGroup = null;
                    }

                    if (rGroup is not null)
                    {
                        viewModel.View.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            SaveFileDialog sfd = new()
                            {
                                Filter = "MP3 Audio File|*.mp3"
                            };

                            if (sfd.ShowDialog() == true)
                            {
                                var fn = sfd.FileName;

                                var fullFn = Path.GetFullPath(fn);

                                var dir = Path.GetDirectoryName(fullFn);
                                var nameNoExt = Path.GetFileNameWithoutExtension(fullFn);
                                var fullFnNoExt = Path.Combine(dir, nameNoExt);

                                YouTubeDownloadView ytd = new(fullFnNoExt, rGroup.Value);

                                if (ytd.ShowDialog() == true)
                                {
                                    var song = SongRepository.Instance.GetOrAddSong(fullFn);

                                    EditSongDetailsView esdv = new(song);

                                    if (esdv.ShowDialog() == true)
                                    {
                                        song = esdv.ResultSong;

                                        SongRepository.Instance.UpdateSong(song, false);
                                    }

                                    var info = viewModel.Playlist;

                                    info.SongFileNames.Insert(dropInfo.InsertIndex, song.FileName);

                                    PlaylistRepository.Instance.UpdatePlaylist(info);

                                    viewModel.Playlist = info;
                                }
                            }
                        }));

                        return;
                    }
                }
            }

            GongSolutions.Wpf.DragDrop.DragDrop.DefaultDropHandler.Drop(dropInfo);
        }
    }
}
