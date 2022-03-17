using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.ViewModels.Pages;
using System.Windows.Controls;
using System.Windows.Input;

namespace BowieD.MusicPlayer.WPF.Views.Pages
{
    /// <summary>
    /// Логика взаимодействия для AlbumPage.xaml
    /// </summary>
    public partial class AlbumPage : Page
    {
        public AlbumPage(Album album, MainWindow mainWindow)
        {
            InitializeComponent();

            this.ViewModel = new AlbumPageViewModel(album, this, mainWindow);

            this.DataContext = this.ViewModel;
        }

        public AlbumPageViewModel ViewModel { get; }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel is null)
                return;

            var index = allSongsListView.SelectedIndex;

            if (index == -1)
                return;

            var song = ViewModel.Album.Songs[index];

            if (song.IsAvailable)
            {
                ViewModel.View.MusicPlayerViewModel.PlaySongFromSource(song, ViewModel.Album);
            }
        }
    }
}
