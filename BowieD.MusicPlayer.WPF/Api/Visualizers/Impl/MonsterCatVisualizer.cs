using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.ViewModels;
using System.Windows.Controls;

namespace BowieD.MusicPlayer.WPF.Api.Visualizers.Impl
{
    public sealed class MonsterCatVisualizer : BaseViewModel, IVisualizer
    {
        public string DisplayName => "Old MonsterCat Visualizer";

        public bool HideDefaultControls => false;
        public bool HideDefaultUpcomingSong => false;

        public Page CreatePage(MainWindowViewModel mwvm, MusicPlayerViewModel mpvm)
        {
            return new Views.MonsterCatPage(this, mwvm, mpvm);
        }

        public void OnStart(Page page)
        {
            if (page is Views.MonsterCatPage dPage)
            {
                dPage.ViewModel.Start();
            }
        }

        public void OnStop(Page page)
        {
            if (page is Views.MonsterCatPage dPage)
            {
                dPage.ViewModel.Stop();
            }
        }
    }
}
