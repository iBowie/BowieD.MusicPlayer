using BowieD.MusicPlayer.WPF.Api;
using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BowieD.MusicPlayer.WPF.ViewModels
{
    public sealed class VisualizerWindowViewModel : BaseViewModelView<VisualizerWindow>
    {
        public VisualizerWindowViewModel(VisualizerWindow view) : base(view)
        {
            SetupVisualizers();
        }

        private Api.IVisualizer? _currentVis;
        private bool _isFullScreen = false;

        public ObservableCollection<IVisualizer> Visualizers { get; } = new();
        public Api.IVisualizer? CurrentVisualizer
        {
            get => _currentVis;
            set
            {
                if (_currentVis is not null)
                {
                    if (View.visualizerFrame.Content is Page cPage)
                        _currentVis.OnStop(cPage);

                    View.visualizerFrame.Source = null;

                    _currentVis = null;
                }

                ChangeProperty(ref _currentVis, value, nameof(CurrentVisualizer));

                if (_currentVis is not null)
                {
                    var page = _currentVis.CreatePage(View.MusicPlayerViewModel.View.ViewModel, View.MusicPlayerViewModel);

                    View.visualizerFrame.Navigate(page);

                    _currentVis.OnStart(page);
                }
                else
                {
                    View.visualizerFrame.Source = null;
                }
            }
        }
        public bool IsFullScreen
        {
            get => _isFullScreen;
            set => ChangeProperty(ref _isFullScreen, value, nameof(IsFullScreen));
        }

        private ICommand? _enterFullScreenCommand, _exitFullScreenCommand;
        public ICommand EnterFullScreenCommand
        {
            get
            {
                if (_enterFullScreenCommand is null)
                {
                    _enterFullScreenCommand = new BaseCommand(() =>
                    {
                        View.WindowState = WindowState.Maximized;
                        View.WindowStyle = WindowStyle.None;
                        View.ResizeMode = ResizeMode.NoResize;
                        View.UseNoneWindowStyle = true;
                        View.IgnoreTaskbarOnMaximize = true;

                        View.Activate();

                        IsFullScreen = true;
                    }, () =>
                    {
                        return !IsFullScreen;
                    });
                }

                return _enterFullScreenCommand;
            }
        }
        public ICommand ExitFullScreenCommand
        {
            get
            {
                if (_exitFullScreenCommand is null)
                {
                    _exitFullScreenCommand = new BaseCommand(() =>
                    {
                        View.fullScreenQueueViewButton.IsChecked = false;

                        View.WindowStyle = WindowStyle.SingleBorderWindow;
                        View.WindowState = WindowState.Normal;
                        View.ResizeMode = ResizeMode.CanResize;
                        View.UseNoneWindowStyle = false;
                        View.ShowTitleBar = true;
                        View.IgnoreTaskbarOnMaximize = false;

                        IsFullScreen = false;
                    }, () =>
                    {
                        return IsFullScreen;
                    });
                }

                return _exitFullScreenCommand;
            }
        }

        private void SetupVisualizers()
        {
            Api.Visualizers.Impl.DefaultImageBackgroundVisualizer defaultVis = new();

            Visualizers.Add(defaultVis);
            Visualizers.Add(new Api.Visualizers.Impl.MonsterCatVisualizer());

            CurrentVisualizer = defaultVis;
        }
    }
}
