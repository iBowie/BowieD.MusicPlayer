using BowieD.MusicPlayer.WPF.ViewModels;
using System;
using System.ComponentModel;
using System.Windows.Controls;

namespace BowieD.MusicPlayer.WPF.Api
{
    public interface IVisualizer : INotifyPropertyChanged
    {
        string DisplayName { get; }
        Page CreatePage(MainWindowViewModel mwvm, MusicPlayerViewModel mpvm);

        bool HideDefaultControls { get; }
        bool HideDefaultUpcomingSong { get; }

        void OnStart(Page page);
        void OnStop(Page page);
    }
}
