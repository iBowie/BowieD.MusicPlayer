using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.ViewModels.Pages;
using System.Windows.Controls;
using System.Windows.Input;

namespace BowieD.MusicPlayer.WPF.Views.Pages
{
    /// <summary>
    /// Логика взаимодействия для ArtistPage.xaml
    /// </summary>
    public partial class ArtistPage : Page
    {
        public ArtistPage(Artist artist, MainWindow mainWindow)
        {
            InitializeComponent();

            this.ViewModel = new ArtistPageViewModel(artist, this, mainWindow);

            this.DataContext = this.ViewModel;
        }

        public ArtistPageViewModel ViewModel { get; }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel is null)
                return;

            var index = allSongsListView.SelectedIndex;

            if (index == -1)
                return;

            var song = ViewModel.Artist.Songs[index];

            if (song.IsAvailable)
            {
                ViewModel.View.MusicPlayerViewModel.PlaySongFromSource(song, ViewModel.Artist);
            }
        }
    }
}
