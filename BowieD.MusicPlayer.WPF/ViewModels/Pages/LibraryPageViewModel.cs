using BowieD.MusicPlayer.WPF.Data;
using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.Views;
using BowieD.MusicPlayer.WPF.Views.Pages;
using System.Collections.ObjectModel;

namespace BowieD.MusicPlayer.WPF.ViewModels.Pages
{
    public sealed class LibraryPageViewModel : BaseViewModelView<MainWindow>
    {
        public LibraryPageViewModel(LibraryPage page, MainWindow view) : base(view)
        {
            this.Page = page;

            var all = PlaylistRepository.Instance.GetAllPlaylists();

            foreach (var p in all)
            {
                Playlists.Add(p);
            }

            var artists = SongRepository.Instance.GetAllArtists();

            foreach (var a in artists)
            {
                Artists.Add(a);
            }

            var albums = SongRepository.Instance.GetAllAlbums();

            foreach (var a in albums)
            {
                Albums.Add(a);
            }
        }

        public LibraryPage Page { get; }

        public ObservableCollection<PlaylistInfo> Playlists { get; } = new();
        public ObservableCollection<Artist> Artists { get; } = new();
        public ObservableCollection<Album> Albums { get; } = new();
    }
}
