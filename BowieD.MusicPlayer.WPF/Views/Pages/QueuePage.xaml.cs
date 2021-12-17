using BowieD.MusicPlayer.WPF.ViewModels.Pages;
using System.Windows.Controls;

namespace BowieD.MusicPlayer.WPF.Views.Pages
{
    /// <summary>
    /// Логика взаимодействия для QueuePage.xaml
    /// </summary>
    public partial class QueuePage : Page
    {
        public QueuePage(QueuePageExtraData data)
        {
            InitializeComponent();

            this.ViewModel = new QueuePageViewModel(this, data.MainWindow);

            this.DataContext = ViewModel;
        }

        public QueuePageViewModel ViewModel { get; }

#if NET6_0_OR_GREATER
        public record struct QueuePageExtraData(MainWindow MainWindow);
#elif NET5_0_OR_GREATER
        public record QueuePageExtraData(MainWindow MainWindow);
#endif
    }
}
