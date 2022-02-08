using BowieD.MusicPlayer.WPF.MVVM;
using System.Windows.Controls;

namespace BowieD.MusicPlayer.WPF.ViewModels.Visualizators
{
    public abstract class VisualizerViewModelBase : BaseViewModel
    {
        public VisualizerViewModelBase(Panel boundPanel, MainWindowViewModel mainWindowViewModel)
        {
            this.MainWindowViewModel = mainWindowViewModel;
            this.BoundPanel = boundPanel;
        }

        public MainWindowViewModel MainWindowViewModel { get; }
        
        public abstract string VisualizerName { get; }
        public Panel BoundPanel { get; }

        public virtual void Setup() { }
        public abstract void Start();
        public abstract void Stop();
    }
}
