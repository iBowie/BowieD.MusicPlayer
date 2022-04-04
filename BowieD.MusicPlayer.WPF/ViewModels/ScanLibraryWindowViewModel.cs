using BowieD.MusicPlayer.WPF.Data;
using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.Views;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BowieD.MusicPlayer.WPF.ViewModels
{
    public sealed class ScanLibraryWindowViewModel : BaseViewModelView<ScanLibraryWindow>
    {
        private CancellationTokenSource? _cts;

        public ScanLibraryWindowViewModel(ScanLibraryWindow view) : base(view)
        {
            if (view.IsLoaded)
            {
                BeginScan();
            }
            else
            {
                view.Loaded += (sender, e) =>
                {
                    BeginScan();
                };
            }

            view.Closed += (sender, e) =>
            {
                EndScan();
            };
        }

        public void BeginScan()
        {
            _cts = new();

            Progress<double> prog = new((val) =>
            {
                if (double.IsNaN(val))
                {
                    IsScanIndeterminate = true;
                }
                else 
                {
                    if (IsScanIndeterminate)
                        IsScanIndeterminate = false;

                    ScanProgress = val;
                }
            });

            Task.Run(async () =>
            {
                await SongRepository.Instance.SearchForMusicAsync(prog, _cts.Token);

                Dispatcher.Invoke(OnEndScan);
            }, _cts.Token);
        }

        public void EndScan()
        {
            if (_cts is null)
                return;

            _cts.Cancel();

            OnEndScan();
        }

        private void OnEndScan()
        {
            View.Close();
        }

        private double _scanProgress;
        public double ScanProgress
        {
            get => _scanProgress;
            private set => ChangeProperty(ref _scanProgress, value, nameof(ScanProgress));
        }

        private bool _scanIsIndeterminate;
        public bool IsScanIndeterminate
        {
            get => _scanIsIndeterminate;
            private set => ChangeProperty(ref _scanIsIndeterminate, value, nameof(IsScanIndeterminate));
        }
    }
}
