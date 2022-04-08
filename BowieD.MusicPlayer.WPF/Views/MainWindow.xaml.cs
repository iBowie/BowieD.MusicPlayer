using BowieD.MusicPlayer.WPF.Api;
using BowieD.MusicPlayer.WPF.Common;
using BowieD.MusicPlayer.WPF.Configuration;
using BowieD.MusicPlayer.WPF.Data;
using BowieD.MusicPlayer.WPF.ViewModels;
using MahApps.Metro.Controls;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BowieD.MusicPlayer.WPF.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            CultureInfo currentCulture = new CultureInfo("en_US");

            CultureInfo.CurrentCulture = currentCulture;
            CultureInfo.CurrentUICulture = currentCulture;
            CultureInfo.DefaultThreadCurrentCulture = currentCulture;
            CultureInfo.DefaultThreadCurrentUICulture = currentCulture;

            App.Current.Resources["AppSettings"] = new AppSettings();

            AppSettings.Instance.Load();

            InitializeComponent();

            ViewModel = new MainWindowViewModel(this);

            DataContext = ViewModel;

            MusicPlayerViewModel = new MusicPlayerViewModel(this);

            musicPlayer.DataContext = MusicPlayerViewModel;

            Memento.RestoreState(this);

            Closing += (sender, e) =>
            {
                if (_allowExit)
                {
                    e.Cancel = false;

                    Memento.SaveState(this);
                }
                else
                {
                    e.Cancel = true;

                    Hide();
                }
            };

            Loaded += (sender, e) =>
            {
                ScanLibraryWindow slw = new();
                slw.ShowDialog();

                MusicPlayerViewModel.SetupIntegrations();
            };

            Color themeColor = VibrantColor.GetVibrantColor(MusicPlayerViewModel.CurrentSong.PictureData);

            AccentColorer.SetAccentColor(themeColor);

            MusicPlayerViewModel.OnTrackChanged += (newTrack) =>
            {
                Color themeColor = VibrantColor.GetVibrantColor(newTrack.PictureData);

                AccentColorer.SetAccentColor(themeColor);
            };
        }

        public MainWindowViewModel ViewModel { get; }
        public MusicPlayerViewModel MusicPlayerViewModel { get; }

        private bool _allowExit = false;

        private void ListView_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = UIElement.MouseWheelEvent,
                    Source = sender
                };
                var parent = ((Control)sender).Parent as UIElement;
                parent?.RaiseEvent(eventArg);
            }
        }

#if WINDOWS10_0_19041_0_OR_GREATER
        internal Windows.Media.SystemMediaTransportControls GetSystemMediaTransportControls()
        {
            return Windows.Media.SystemMediaTransportControlsInterop.GetForWindow(this.CriticalHandle);
        }
#endif

        public void CloseCompletely()
        {
            _allowExit = true;

            Close();
        }
    }
}
