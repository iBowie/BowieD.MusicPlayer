using BowieD.MusicPlayer.WPF.Data;
using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.Views;
using BowieD.MusicPlayer.WPF.Views.Pages;
using System.IO;
using System.Linq;
using System.Windows;
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
            _reReadTagsCommand;

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

                    MessageBox.Show($"Tags re-read complete!\n{cnt} songs affected");
                });
            }
        }
    }
}
