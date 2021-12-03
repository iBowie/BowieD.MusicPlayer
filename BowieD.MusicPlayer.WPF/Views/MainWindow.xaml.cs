using BowieD.MusicPlayer.WPF.ViewModels;
using MahApps.Metro.Controls;

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
    }
}
