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
        }

        public LibraryPage Page { get; }

        private readonly ObservableCollection<PlaylistInfo> _playlists = new();

        public ObservableCollection<PlaylistInfo> Playlists
        {
            get => _playlists;
        }
    }
}
