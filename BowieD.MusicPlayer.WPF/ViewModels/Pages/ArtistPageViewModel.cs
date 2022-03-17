using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.Views;
using BowieD.MusicPlayer.WPF.Views.Pages;

namespace BowieD.MusicPlayer.WPF.ViewModels.Pages
{
    public sealed class ArtistPageViewModel : BaseViewModelView<MainWindow>
    {
        public ArtistPageViewModel(Artist artist, ArtistPage page, MainWindow view) : base(view)
        {
            Artist = artist;
            Page = page;
        }

        public Artist Artist { get; }
        public ArtistPage Page { get; }
    }
}
