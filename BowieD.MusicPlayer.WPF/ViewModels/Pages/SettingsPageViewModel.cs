﻿using BowieD.MusicPlayer.WPF.Configuration;
using BowieD.MusicPlayer.WPF.Data;
using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.Views;
using BowieD.MusicPlayer.WPF.Views.Pages;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace BowieD.MusicPlayer.WPF.ViewModels.Pages
{
    public sealed class SettingsPageViewModel : BaseViewModelView<MainWindow>
    {
        public SettingsPageViewModel(SettingsPage page, MainWindow view) : base(view)
        {
            Page = page;
        }

        public SettingsPage Page { get; }

        private ICommand? _locateMissingFilesCommand,
            _reReadTagsCommand, _scanLibraryCommand,
            _removeDeletedSongsCommand, _addCustomMusicFolderCommand,
            _deleteCustomMusicFoldersCommand, _pickCustomMusicFolderCommand,
            _saveCommand, _loadDefaultsCommand;

        public ICommand LocateMissingFilesCommand
        {
            get
            {
                return _locateMissingFilesCommand ??= new BaseCommand(() =>
                {
                    var allSongs = SongRepository.Instance.GetAllSongs();

                    int left = allSongs.Count(d => !File.Exists(d.FileName));

                    var res = MessageBox.Show($"Are you sure you want to relocate missing files?\nYou will have to locate {left} files.", "File relocation", MessageBoxButton.YesNo);

                    if (res != MessageBoxResult.Yes)
                        return;

                    var mp = View.MusicPlayerViewModel;

                    mp.CurrentSongSource = null;
                    mp.UserSongQueue.Clear();
                    mp.SongQueue.Clear();
                    mp.SongHistory.Clear();
                    mp.SetCurrentSong(Models.Song.EMPTY, false);

                    int cntRelocated = 0, cntSkipped = 0;

                    foreach (var song in allSongs)
                    {
                        if (File.Exists(song.FileName))
                            continue;

                        Microsoft.Win32.OpenFileDialog ofd = new()
                        {
                            Title = $"Locate '{song.Title}' - '{song.Artist}' :: {song.Album} ({song.Year}) [{left} left]",
                            Filter = FileTool.CreateFileDialogFilter(Common.BassWrapper.SupportedExtensions),
                            Multiselect = false,
                            FileName = song.FileName,
                        };

                        if (ofd.ShowDialog() == true && FileTool.CheckFileValid(ofd.FileName, Common.BassWrapper.SupportedExtensions))
                        {
                            song.UpdateFileName(ofd.FileName);

                            SongRepository.Instance.UpdateSong(song, false);

                            cntRelocated++;
                        }
                        else
                        {
                            cntSkipped++;
                        }

                        left--;
                    }

                    foreach (var s in View.MusicPlayerViewModel.SongQueue)
                        s.UpdateFromDatabase();

                    foreach (var s in View.MusicPlayerViewModel.SongHistory)
                        s.UpdateFromDatabase();

                    foreach (var s in View.MusicPlayerViewModel.UserSongQueue)
                        s.UpdateFromDatabase();

                    MessageBox.Show($"File relocation complete!\n{cntRelocated} songs were relocated, {cntSkipped} were skipped");
                });
            }
        }
        public ICommand ReReadTagsCommand
        {
            get
            {
                return _reReadTagsCommand ??= new BaseCommand(() =>
                {
                    var res = MessageBox.Show("Are you sure you want to re-read tags?", "Re-read tags", MessageBoxButton.YesNo);

                    if (res != MessageBoxResult.Yes)
                        return;

                    var allSongs = SongRepository.Instance.GetAllSongs();

                    int cnt = 0;

                    foreach (var song in allSongs)
                    {
                        if (!song.IsAvailable || song.IsEmpty)
                            continue;

                        SongRepository.Instance.UpdateSong(song, true);

                        cnt++;
                    }

                    foreach (var s in View.MusicPlayerViewModel.SongQueue)
                        s.UpdateFromDatabase();

                    foreach (var s in View.MusicPlayerViewModel.SongHistory)
                        s.UpdateFromDatabase();

                    foreach (var s in View.MusicPlayerViewModel.UserSongQueue)
                        s.UpdateFromDatabase();

                    CoverCacheRepository.Instance.ClearCovers();

                    MessageBox.Show($"Tags re-read complete!\n{cnt} songs affected");
                });
            }
        }
        public ICommand ScanLibraryCommand
        {
            get
            {
                return _scanLibraryCommand ??= new BaseCommand(() =>
                {
                    ScanLibraryWindow slw = new();
                    slw.ShowDialog();
                });
            }
        }
        public ICommand RemoveDeletedSongsCommand
        {
            get
            {
                return _removeDeletedSongsCommand ??= new BaseCommand(() =>
                {
                    var res = MessageBox.Show($"Are you sure you want to remove deleted songs?", "Remove deleted songs", MessageBoxButton.YesNo);

                    if (res != MessageBoxResult.Yes)
                        return;

                    var mp = View.MusicPlayerViewModel;

                    mp.CurrentSongSource = null;
                    mp.UserSongQueue.Clear();
                    mp.SongQueue.Clear();
                    mp.SongHistory.Clear();
                    mp.SetCurrentSong(Models.Song.EMPTY, false);

                    var allSongs = SongRepository.Instance.GetAllSongs();

                    for (int i = allSongs.Count - 1; i >= 0; i--)
                    {
                        var song = allSongs[i];

                        if (!File.Exists(song.FileName))
                        {
                            SongRepository.Instance.RemoveSong(song);
                        }
                    }

                    var pls = PlaylistRepository.Instance.GetAllPlaylists();

                    foreach (var pl in pls)
                    {
                        var songs = pl.SongFileNames;
                        bool needsUpdate = false;

                        for (int i = songs.Count - 1; i >= 0; i--)
                        {
                            var song = songs[i];

                            if (!File.Exists(song))
                            {
                                songs.RemoveAt(i);
                                needsUpdate = true;
                            }
                        }

                        if (needsUpdate)
                            PlaylistRepository.Instance.UpdatePlaylist(pl);
                    }
                });
            }
        }
        public ICommand AddCustomMusicFolderCommand
        {
            get
            {
                return _addCustomMusicFolderCommand ??= new GenericCommand<string>((p) =>
                {
                    if (p is null)
                        throw new ArgumentNullException(nameof(p));

                    AppSettings.Instance.CustomLibraryFolders.Add(p);
                }, (p) =>
                {
                    if (string.IsNullOrWhiteSpace(p))
                        return false;

                    if (!Directory.Exists(p))
                        return false;

                    return true;
                });
            }
        }
        public ICommand DeleteCustomMusicFoldersCommand
        {
            get
            {
                return _deleteCustomMusicFoldersCommand ??= new GenericCommand<IList>((lst) =>
                {
                    if (lst is null)
                        throw new ArgumentNullException(nameof(lst));

                    var casted = lst.Cast<string>().ToList();

                    foreach (var item in casted)
                    {
                        AppSettings.Instance.CustomLibraryFolders.Remove(item);
                    }
                }, (lst) =>
                {
                    if (lst is null)
                        return false;

                    foreach (var item in lst)
                    {
                        if (item is not string)
                            return false;
                    }

                    return true;
                });
            }
        }
        public ICommand PickCustomMusicFolderCommand
        {
            get
            {
                return _pickCustomMusicFolderCommand ??= new BaseCommand(() =>
                {
                    Ookii.Dialogs.Wpf.VistaFolderBrowserDialog ofd = new()
                    {
                        Multiselect = true,
                    };

                    if (ofd.ShowDialog() == true)
                    {
                        foreach (var folder in ofd.SelectedPaths)
                        {
                            AppSettings.Instance.CustomLibraryFolders.Add(folder);
                        }
                    }
                });
            }
        }
        public ICommand SaveCommand
        {
            get
            {
                return _saveCommand ??= new BaseCommand(() =>
                {
                    AppSettings.Instance.Save();
                });
            }
        }
        public ICommand LoadDefaultsCommand
        {
            get
            {
                return _loadDefaultsCommand ??= new BaseCommand(() =>
                {
                    AppSettings.Instance.LoadDefaults();
                });
            }
        }
    }
}
