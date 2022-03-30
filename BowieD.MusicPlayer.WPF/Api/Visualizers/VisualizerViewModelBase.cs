using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.ViewModels;
using System.Windows.Controls;

namespace BowieD.MusicPlayer.WPF.Api.Visualizers
{
    public abstract class VisualizerViewModelBase<TPage, TVisualizer> : BaseViewModel where TPage : Page where TVisualizer : IVisualizer
    {
        public VisualizerViewModelBase(TPage page, TVisualizer visualizer, MainWindowViewModel mainWindowViewModel, MusicPlayerViewModel musicPlayerViewModel)
        {
            this.Page = page;
            this.MainWindowViewModel = mainWindowViewModel;
            this.MusicPlayerViewModel = musicPlayerViewModel;
            this.Visualizer = visualizer;
        }

        public MainWindowViewModel MainWindowViewModel { get; }
        public MusicPlayerViewModel MusicPlayerViewModel { get; }

        public TPage Page { get; }
        public TVisualizer Visualizer { get; }

        public abstract void Start();
        public abstract void Stop();
    }
}
