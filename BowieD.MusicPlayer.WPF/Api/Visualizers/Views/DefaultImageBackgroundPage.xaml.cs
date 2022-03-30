using BowieD.MusicPlayer.WPF.Api.Visualizers.ViewModels;
using BowieD.MusicPlayer.WPF.Api.Visualizers.Impl;
using BowieD.MusicPlayer.WPF.ViewModels;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BowieD.MusicPlayer.WPF.Api.Visualizers.Views
{
    /// <summary>
    /// Логика взаимодействия для DefaultImageBackgroundPage.xaml
    /// </summary>
    public partial class DefaultImageBackgroundPage : Page
    {
        public DefaultImageBackgroundPage(DefaultImageBackgroundVisualizer visualizer, MainWindowViewModel mwvm, MusicPlayerViewModel mpvm)
        {
            InitializeComponent();

            this.ViewModel = new(this, visualizer, mwvm, mpvm);

            this.DataContext = this.ViewModel;
        }

        public DefaultImageBackgroundViewModel ViewModel { get; }

        private void visualizerGrid_default_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Handled)
                return;

            var obj = e.Data;

            string format = DataFormats.FileDrop;

            if (obj.GetDataPresent(format))
            {
                string[] files = (string[])obj.GetData(format);

                if (files.Length > 0)
                {
                    ViewModel.SetBackground(files.Where(fn => FileTool.CheckFileValid(fn, ImageTool.SupportedImageExtensions)).ToArray());
                }
            }
        }
    }
}
