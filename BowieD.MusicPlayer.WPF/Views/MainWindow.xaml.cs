using BowieD.MusicPlayer.WPF.Data;
using BowieD.MusicPlayer.WPF.ViewModels;
using MahApps.Metro.Controls;
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

            MusicPlayerViewModel = new MusicPlayerViewModel(this);

            musicPlayer.DataContext = MusicPlayerViewModel;
        }

        public MainWindowViewModel ViewModel { get; }
        public PlaylistViewModel PlaylistViewModel { get; }
        public MusicPlayerViewModel MusicPlayerViewModel { get; }

        private void ListView_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (PlaylistViewModel.PlaylistInfo.IsEmpty)
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
                    var info = PlaylistViewModel.PlaylistInfo;

                    foreach (var fn in files)
                    {
                        var song = SongRepository.Instance.GetOrAddSong(fn);

                        info.SongIDs.Add(song.ID);
                    }

                    PlaylistRepository.Instance.UpdatePlaylist(info);

                    ViewModel.ObtainPlaylists();

                    ViewModel.SelectedPlaylist = info;
                }
            }
        }

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
    }
}
