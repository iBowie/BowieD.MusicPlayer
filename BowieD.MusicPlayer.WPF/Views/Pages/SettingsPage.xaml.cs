using BowieD.MusicPlayer.WPF.ViewModels.Pages;
using System.Windows.Controls;

namespace BowieD.MusicPlayer.WPF.Views.Pages
{
    /// <summary>
    /// Логика взаимодействия для SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page
    {
        public SettingsPage(MainWindow mainWindow)
        {
            InitializeComponent();

            ViewModel = new(this, mainWindow);

            DataContext = ViewModel;
        }

        public SettingsPageViewModel ViewModel { get; }
    }
}
