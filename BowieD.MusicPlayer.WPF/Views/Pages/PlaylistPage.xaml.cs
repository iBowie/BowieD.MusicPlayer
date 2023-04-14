using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.ViewModels.Pages;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BowieD.MusicPlayer.WPF.Views.Pages
{
    /// <summary>
    /// Логика взаимодействия для PlaylistPage.xaml
    /// </summary>
    public partial class PlaylistPage : Page
    {
        public PlaylistPage(PlaylistPageExtraData data)
        {
            InitializeComponent();

            ViewModel = new(this, data.MainWindow);

            ViewModel.Playlist = data.Playlist;

            DataContext = ViewModel;
        }

        public PlaylistViewModel? ViewModel { get; private set; }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel is null)
                return;

            if (ViewModel.Playlist.IsEmpty)
                return;

            var index = playlistSongsListView.SelectedIndex;

            if (index == -1)
                return;

            var song = ViewModel.Playlist.Songs[index];

            if (song.IsAvailable)
            {
                ViewModel.View.MusicPlayerViewModel.PlaySongFromSource(song, ViewModel.Playlist);
            }
        }

        private void playlistSongsListView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled)
                return;

            if (ViewModel is null)
                return;

            if (ViewModel.Playlist.IsEmpty)
                return;

            switch (e.Key)
            {
                case Key.Delete:
                    {
                        var selectedItems = playlistSongsListView.SelectedItems;

                        List<object> list = new();

                        foreach (var si in selectedItems)
                            list.Add(si);

                        foreach (var si in list)
                        {
                            if (si is Song song)
                            {
                                ViewModel.Songs.Remove(song);
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


#if NET6_0_OR_GREATER
        public record struct PlaylistPageExtraData(Playlist Playlist, MainWindow MainWindow);
#elif NET5_0_OR_GREATER
        public record PlaylistPageExtraData(Playlist Playlist, MainWindow MainWindow);
#endif
    }
}
