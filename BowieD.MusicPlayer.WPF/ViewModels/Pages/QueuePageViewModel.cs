using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.Views;
using BowieD.MusicPlayer.WPF.Views.Pages;
using System.Collections.ObjectModel;

namespace BowieD.MusicPlayer.WPF.ViewModels.Pages
{
    public sealed class QueuePageViewModel : BaseViewModelView<MainWindow>
    {
        public QueuePageViewModel(QueuePage page, MainWindow view) : base(view)
        {
            this.Page = page;
        }

        public QueuePage Page { get; }

        public Song CurrentSong => View.MusicPlayerViewModel.CurrentSong;
        public ObservableCollection<Song> UserSongQueue => View.MusicPlayerViewModel.UserSongQueue;
        public ObservableCollection<Song> SongQueue => View.MusicPlayerViewModel.SongQueue;
        public ObservableCollection<Song> SongHistory => View.MusicPlayerViewModel.SongHistory;
    }
}
