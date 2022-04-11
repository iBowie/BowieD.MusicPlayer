using BowieD.MusicPlayer.WPF.Data;
using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.Views;
using BowieD.MusicPlayer.WPF.Views.Pages;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BowieD.MusicPlayer.WPF.ViewModels.Pages
{
    public sealed class AllSongsPageViewModel : BaseViewModelView<MainWindow>
    {
        private static readonly IList<Song> _songs = new List<Song>();

        public AllSongsPageViewModel(AllSongsPage page, MainWindow view) : base(view)
        {
            Page = page;
        }

        public AllSongsPage Page { get; }

        public IList<Song> Songs => SongRepository.Instance.GetAllSongs(_songs);
    }
}
