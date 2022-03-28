using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.ViewModels;
using System.Windows.Controls;

namespace BowieD.MusicPlayer.WPF.Api.Visualizers.Impl
{
    public sealed class DefaultImageBackgroundVisualizer : BaseViewModel, IVisualizer
    {
        public string DisplayName => "Default Image Background";

        public bool HideDefaultControls => false;
        public bool HideDefaultUpcomingSong => false;

        public Page CreatePage(MainWindowViewModel mwvm, MusicPlayerViewModel mpvm)
        {
            return new Views.DefaultImageBackgroundPage(this, mwvm, mpvm);
        }

        public void OnStart(Page page)
        {
            if (page is Views.DefaultImageBackgroundPage dPage)
            {
                dPage.ViewModel.Start();
            }
        }

        public void OnStop(Page page)
        {
            if (page is Views.DefaultImageBackgroundPage dPage)
            {
                dPage.ViewModel.Stop();
            }
        }
    }
}
