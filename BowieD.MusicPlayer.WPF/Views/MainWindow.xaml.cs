using BowieD.MusicPlayer.WPF.Common;
using BowieD.MusicPlayer.WPF.ViewModels;
using BowieD.MusicPlayer.WPF.ViewModels.Visualizators;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

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

                SetupVisualizers();
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

        private void SetupVisualizers()
        {
            DefaultBackgroundVisualizerViewModel defaultVis = new(ViewModel);
            MonsterCatVisualizerViewModel monsterVis = new(ViewModel);
            
            visualizerGrid_default.DataContext = defaultVis;
            visualizerGrid_monsterCat.DataContext = monsterVis;

            defaultVis.Setup();
            monsterVis.Setup();

            _currentVisualizer = defaultVis;
            defaultVis.Start();
        }

        private VisualizerViewModelBase? _currentVisualizer;

        private void VisualizerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_currentVisualizer is not null)
            {
                _currentVisualizer.Stop();
                _currentVisualizer = null;
            }

            switch (visualizerComboBox.SelectedIndex)
            {
                case 0: // default
                    {
                        visualizerGrid_monsterCat.Visibility = Visibility.Collapsed;
                        visualizerGrid_default.Visibility = Visibility.Visible;
                        defaultVisualizerText.Visibility = Visibility.Visible;

                        _currentVisualizer = visualizerGrid_default.DataContext as VisualizerViewModelBase;
                    }
                    break;
                case 1: // monstercat
                    {
                        visualizerGrid_default.Visibility = Visibility.Collapsed;
                        defaultVisualizerText.Visibility = Visibility.Collapsed;
                        visualizerGrid_monsterCat.Visibility = Visibility.Visible;

                        _currentVisualizer = visualizerGrid_monsterCat.DataContext as VisualizerViewModelBase;
                    }
                    break;
            }

            if (_currentVisualizer is not null)
            {
                _currentVisualizer.Start();
            }
        }
    }
}
