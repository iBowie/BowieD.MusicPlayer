using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.Views;
using BowieD.MusicPlayer.WPF.Views.Pages;

namespace BowieD.MusicPlayer.WPF.ViewModels.Pages
{
    public sealed class AlbumPageViewModel : BaseViewModelView<MainWindow>
    {
        public AlbumPageViewModel(Album album, AlbumPage page, MainWindow view) : base(view)
        {
            Album = album;
            Page = page;
        }

        public Album Album { get; }
        public AlbumPage Page { get; }
    }
}
