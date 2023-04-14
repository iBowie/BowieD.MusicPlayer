using BowieD.MusicPlayer.WPF.Configuration;
using BowieD.MusicPlayer.WPF.Data;
using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.Views;
using System;
using System.IO;
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
                if (AppSettings.Instance.LibraryScanMyMusicFolder)
                {
                    string myMusicFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

                    if (Directory.Exists(myMusicFolder))
                        await SongRepository.Instance.SearchForMusicAsync(myMusicFolder, prog, _cts.Token);
                }

                if (AppSettings.Instance.LibraryScanCommonMusicFolder)
                {
                    string commonMusicFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonMusic);

                    if (Directory.Exists(commonMusicFolder))
                        await SongRepository.Instance.SearchForMusicAsync(commonMusicFolder, prog, _cts.Token);
                }

                foreach (var customSrc in AppSettings.Instance.CustomLibraryFolders)
                {
                    if (Directory.Exists(customSrc))
                        await SongRepository.Instance.SearchForMusicAsync(customSrc, prog, _cts.Token);
                }

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
