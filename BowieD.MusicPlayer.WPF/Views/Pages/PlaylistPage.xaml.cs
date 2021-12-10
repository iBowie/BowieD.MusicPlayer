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

            ViewModel.PlaylistInfo = data.Playlist;

            DataContext = ViewModel;
        }

        public PlaylistViewModel? ViewModel { get; private set; }

        private void ListView_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = UIElement.MouseWheelEvent,
                    Source = sender
                };
                var parent = ((Control)sender).Parent as UIElement;
                parent?.RaiseEvent(eventArg);
            }
        }

        private void playlistScrollViewerContent_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            const double REQUIRED_OFFSET = 300.0;

            if (e.VerticalChange == 0)
                return;

            if (e.VerticalOffset >= REQUIRED_OFFSET)
            {
                playlistScrollViewerHeaderWhenScrolled.Visibility = Visibility.Visible;
            }
            else
            {
                playlistScrollViewerHeaderWhenScrolled.Visibility = Visibility.Collapsed;
            }
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel is null)
                return;

            if (ViewModel.PlaylistInfo.IsEmpty)
                return;

            var index = playlistSongsListView.SelectedIndex;

            if (index == -1)
                return;

            var song = ViewModel.Playlist.Songs[index];

            if (song.IsAvailable)
            {
                ViewModel.View.MusicPlayerViewModel.PlaySongFromPlaylist(song, ViewModel.Playlist);
            }
        }

        private void playlistSongsListView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled)
                return;

            if (ViewModel is null)
                return;

            if (ViewModel.PlaylistInfo.IsEmpty)
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
        public record struct PlaylistPageExtraData(PlaylistInfo Playlist, MainWindow MainWindow);
#elif NET5_0_OR_GREATER
        public record PlaylistPageExtraData(PlaylistInfo Playlist, MainWindow MainWindow);
#endif
    }
}
