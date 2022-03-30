using BowieD.MusicPlayer.WPF.Api;
using BowieD.MusicPlayer.WPF.ViewModels;
using System.Windows.Controls;

namespace BowieD.MusicPlayer.WPF.Views
{
    /// <summary>
    /// Логика взаимодействия для VisualizerWindow.xaml
    /// </summary>
    public partial class VisualizerWindow : MahApps.Metro.Controls.MetroWindow
    {
        public VisualizerWindow(MusicPlayerViewModel musicPlayerViewModel)
        {
            this.MusicPlayerViewModel = musicPlayerViewModel;

            InitializeComponent();

            this.ViewModel = new(this);

            this.DataContext = this.ViewModel;
        }

        public VisualizerWindowViewModel ViewModel { get; }
        public MusicPlayerViewModel MusicPlayerViewModel { get; }
    }
}
