using BowieD.MusicPlayer.WPF.Api.Visualizers.Impl;
using BowieD.MusicPlayer.WPF.Api.Visualizers.ViewModels;
using BowieD.MusicPlayer.WPF.ViewModels;
using System.Windows.Controls;

namespace BowieD.MusicPlayer.WPF.Api.Visualizers.Views
{
    /// <summary>
    /// Логика взаимодействия для MonsterCatPage.xaml
    /// </summary>
    public partial class MonsterCatPage : Page
    {
        public MonsterCatPage(MonsterCatVisualizer visualizer, MainWindowViewModel mwvm, MusicPlayerViewModel mpvm)
        {
            InitializeComponent();

            this.ViewModel = new(this, visualizer, mwvm, mpvm);

            this.DataContext = this.ViewModel;
        }

        public MonsterCatViewModel ViewModel { get; }
    }
}
