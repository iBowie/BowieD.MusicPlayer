using BowieD.MusicPlayer.WPF.ViewModels;
using MahApps.Metro.Controls;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

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

            InitializeComponent();

            ViewModel = new MainWindowViewModel(this);

            DataContext = ViewModel;

            MusicPlayerViewModel = new MusicPlayerViewModel(this);

            musicPlayer.DataContext = MusicPlayerViewModel;

            Memento.RestoreState(this);

            Closing += (sender, e) =>
            {
                Memento.SaveState(this);
            };

            Loaded += (sender, e) =>
            {
                MusicPlayerViewModel.SetupIntegrations();
            };
        }

        public MainWindowViewModel ViewModel { get; }
        public MusicPlayerViewModel MusicPlayerViewModel { get; }

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

        private void fullScreenViewGrid_Drop(object sender, DragEventArgs e)
        {
            if (!MusicPlayerViewModel.IsFullScreen)
                return;

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

#if WINDOWS10_0_19041_0_OR_GREATER
        internal Windows.Media.SystemMediaTransportControls GetSystemMediaTransportControls()
        {
            return Windows.Media.SystemMediaTransportControlsInterop.GetForWindow(this.CriticalHandle);
        }
#endif
    }
}
