using BowieD.MusicPlayer.WPF.ViewModels;
using MahApps.Metro.Controls;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BowieD.MusicPlayer.WPF.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new MainWindowViewModel(this);

            DataContext = ViewModel;

            PlaylistViewModel = new PlaylistViewModel(this);

            playlistScrollViewer.DataContext = PlaylistViewModel;

            PlaylistViewModel.PlaylistInfo = Models.PlaylistInfo.EMPTY;

            MusicPlayerViewModel = new MusicPlayerViewModel(this);

            musicPlayer.DataContext = MusicPlayerViewModel;

            Memento.RestoreState(this);

            Closing += (sender, e) =>
            {
                Memento.SaveState(this);
            };
        }

        public MainWindowViewModel ViewModel { get; }
        public PlaylistViewModel PlaylistViewModel { get; }
        public MusicPlayerViewModel MusicPlayerViewModel { get; }

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
            if (PlaylistViewModel.PlaylistInfo.IsEmpty)
                return;

            var index = playlistSongsListView.SelectedIndex;

            if (index == -1)
                return;

            var song = PlaylistViewModel.Playlist.Songs[index];

            if (song.IsAvailable)
            {
                MusicPlayerViewModel.PlaySongFromPlaylist(song, PlaylistViewModel.Playlist);
            }
        }

        private void playlistSongsListView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (PlaylistViewModel.PlaylistInfo.IsEmpty)
                return;

            var index = playlistSongsListView.SelectedIndex;

            if (index == -1)
                return;

            PlaylistViewModel.Songs.RemoveAt(index);
        }

        private void fullScreenViewGrid_Drop(object sender, DragEventArgs e)
        {
            if (!MusicPlayerViewModel.IsFullScreen)
                return;

            if (e.Handled)
                return;

            var obj = e.Data;

            string format = DataFormats.FileDrop;

            if (obj.GetDataPresent(format))
            {
                string[] files = (string[])obj.GetData(format);

                if (files.Length > 0)
                {
                    ViewModel.SetBackground(files.Where(fn => FileTool.CheckFileValid(fn, ImageTool.SupportedImageExtensions)).ToArray());
                }
            }
        }
    }
}
