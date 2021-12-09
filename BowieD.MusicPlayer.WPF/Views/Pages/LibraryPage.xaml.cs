using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.ViewModels.Pages;
using System.Windows;
using System.Windows.Controls;

namespace BowieD.MusicPlayer.WPF.Views.Pages
{
    /// <summary>
    /// Логика взаимодействия для LibraryPage.xaml
    /// </summary>
    public partial class LibraryPage : Page
    {
        public LibraryPage(LibraryPageExtraData data)
        {
            InitializeComponent();

            ViewModel = new LibraryPageViewModel(this, data.MainWindow);

            DataContext = ViewModel;
        }

        public LibraryPageViewModel ViewModel { get; }

        private void b_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement frameworkElement && frameworkElement.DataContext is PlaylistInfo pInfo)
            {
                ViewModel.View.ViewModel.SelectedPlaylist = pInfo;
            }
        }

#if NET6_0_OR_GREATER
        public record struct LibraryPageExtraData(MainWindow MainWindow);
#elif NET5_0_OR_GREATER
        public record LibraryPageExtraData(MainWindow MainWindow);
#endif
    }
}
