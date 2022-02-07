using BowieD.MusicPlayer.WPF.MVVM;

namespace BowieD.MusicPlayer.WPF.ViewModels.Visualizators
{
    public abstract class VisualizerViewModelBase : BaseViewModel
    {
        protected static readonly float[] _fft = new float[1024];

        public VisualizerViewModelBase(MainWindowViewModel mainWindowViewModel)
        {
            this.MainWindowViewModel = mainWindowViewModel;
        }

        public MainWindowViewModel MainWindowViewModel { get; }

        public virtual void Setup() { }
        public abstract void Start();
        public abstract void Stop();
    }
}
