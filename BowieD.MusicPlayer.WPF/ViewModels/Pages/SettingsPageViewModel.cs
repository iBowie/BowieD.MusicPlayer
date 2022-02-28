using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.Views;
using BowieD.MusicPlayer.WPF.Views.Pages;

namespace BowieD.MusicPlayer.WPF.ViewModels.Pages
{
    public sealed class SettingsPageViewModel : BaseViewModelView<MainWindow>
    {
        public SettingsPageViewModel(SettingsPage page, MainWindow view) : base(view)
        {
            Page = page;
        }

        public SettingsPage Page { get; }
    }
}
