using BowieD.MusicPlayer.WPF.Data;
using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.Views;
using BowieD.MusicPlayer.WPF.Views.Pages;
using System.Collections.ObjectModel;

namespace BowieD.MusicPlayer.WPF.ViewModels.Pages
{
    public sealed class AllSongsPageViewModel : BaseViewModelView<MainWindow>
    {
        public AllSongsPageViewModel(AllSongsPage page, MainWindow view) : base(view)
        {
            Page = page;

            var allSongs = SongRepository.Instance.GetAllSongs();

            foreach (var a in allSongs)
                Songs.Add(a);
        }

        public AllSongsPage Page { get; }

        public ObservableCollection<Song> Songs { get; } = new();
    }
}
