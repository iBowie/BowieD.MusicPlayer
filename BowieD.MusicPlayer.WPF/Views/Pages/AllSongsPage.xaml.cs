using BowieD.MusicPlayer.WPF.Data;
using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.ViewModels.Pages;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace BowieD.MusicPlayer.WPF.Views.Pages
{
    /// <summary>
    /// Логика взаимодействия для AllSongsPage.xaml
    /// </summary>
    public partial class AllSongsPage : Page
    {
        public AllSongsPage(MainWindow mainWindow)
        {
            InitializeComponent();

            ViewModel = new(this, mainWindow);

            DataContext = ViewModel;
        }

        public AllSongsPageViewModel ViewModel { get; }

        public sealed class AllSongsSource : ISongSource
        {
            public static readonly AllSongsSource Instance = new();

            private AllSongsSource() { }

            public string SourceName => "All songs";
            public IReadOnlyCollection<Song> GetSongs(Song currentSong)
            {
                var allSongs = SongRepository.Instance.GetAllSongs();

                int index = -1;

                for (int i = 0; i < allSongs.Count; i++)
                {
                    if (allSongs[i].ID == currentSong.ID)
                    {
                        index = i;
                        break;
                    }
                }

                if (index >= 0)
                {
                    return new ReadOnlyCollection<Song>(allSongs.Skip(index + 1).Concat(allSongs.Take(index)).ToList());
                }

                return new ReadOnlyCollection<Song>(allSongs);
            }
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel is null)
                return;

            var index = allSongsListView.SelectedIndex;

            if (index == -1)
                return;

            var song = ViewModel.Songs[index];

            if (song.IsAvailable)
            {
                ViewModel.View.MusicPlayerViewModel.PlaySongFromSource(song, AllSongsSource.Instance);
            }
        }

        private void allSongsListView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled)
                return;

            switch (e.Key)
            {
                case Key.Delete:
                    {
                        var selectedItems = allSongsListView.SelectedItems;

                        List<object> list = new();

                        foreach (var si in selectedItems)
                            list.Add(si);

                        foreach (var si in list)
                        {
                            if (si is Song song)
                            {
                                ViewModel.Songs.Remove(song);

                                SongRepository.Instance.RemoveSong(song);
                            }
                        }

                        e.Handled = true;
                    }
                    break;
                case Key.Enter:
                    {
                        // play songs by adding to queue selected ones

                        e.Handled = true;
                    }
                    break;
            }
        }
    }
}
