using BowieD.MusicPlayer.WPF.Configuration;
using BowieD.MusicPlayer.WPF.MVVM;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Un4seen.Bass;

namespace BowieD.MusicPlayer.WPF.Common
{
    public sealed class BassWrapper : BaseViewModel
    {
        private int _handle;
        private bool _hasInitDefaultDevice;

        public const int FREQ_HZ = 44100;

        public BassWrapper(System.Windows.Threading.DispatcherTimer propUpdateTimer)
        {
            propUpdateTimer.Tick += (sender, e) =>
            {
                TriggerPropertyChanged(nameof(Position));
                TriggerPropertyChanged(nameof(Duration));
            };

            AppSettings.Instance.StartTrackingSetting((settings) =>
            {
                UpdateVolume();
            }, nameof(AppSettings.EnableReplayGain));

            AppSettings.Instance.StartTrackingSetting((settings) =>
            {
                FadeDuration = settings.SmoothFadeDuration;
            }, nameof(AppSettings.SmoothFadeDuration));
        }

        public bool Init()
        {
            if (!_hasInitDefaultDevice)
            {
                _hasInitDefaultDevice = Bass.BASS_Init(-1, FREQ_HZ, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
            }

            return _hasInitDefaultDevice;
        }

        public void LoadFile(string fileName)
        {
            Init();

            if (_handle != 0)
            {
                Stop();
            }

            _handle = Bass.BASS_StreamCreateFile(fileName, 0, 0, BASSFlag.BASS_DEFAULT | BASSFlag.BASS_SAMPLE_FLOAT);

            try
            {
                using var tags = TagLib.File.Create(fileName);

                if (!TrySetReplayGain(tags.Tag.ReplayGainAlbumGain, tags.Tag.ReplayGainAlbumPeak))
                {
                    if (!TrySetReplayGain(tags.Tag.ReplayGainTrackGain, tags.Tag.ReplayGainTrackPeak))
                    {
                        ReplayGainVolume = 1f;
                    }
                }
            }
            catch
            {
                ReplayGainVolume = 1f;
            }

            UpdateVolume();
        }

        private bool TrySetReplayGain(double gain, double peak)
        {
            if (double.IsNaN(gain))
                return false;

            peak = double.IsNaN(peak) ? 1.0 : peak;

            double res = Math.Pow(10, gain / 20.0);

            if (double.IsFinite(res))
            {
                ReplayGainVolume = (float)res;
                return true;
            }
            else
            {
                ReplayGainVolume = 1f;
                return false;
            }
        }

        public bool Play()
        {
            AnimateVolume = 0f;

            if (Bass.BASS_ChannelPlay(_handle, true))
            {
                BeginAnimateVolumeUp();

                return true;
            }
            else
            {
                return false;
            }
        }

        public void Pause()
        {
            BeginAnimateVolumeDown(() =>
            {
                Bass.BASS_ChannelPause(_handle);
            });
        }

        public bool Resume()
        {
            if (Bass.BASS_ChannelPlay(_handle, false))
            {
                BeginAnimateVolumeUp();

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Stop()
        {
            return Bass.BASS_ChannelStop(_handle) && Bass.BASS_StreamFree(_handle);
        }

        public BASSActive State => Bass.BASS_ChannelIsActive(_handle);

        private void UpdateVolume()
        {
            Bass.BASS_ChannelSetAttribute(_handle, BASSAttribute.BASS_ATTRIB_VOL, TotalVolume);
        }

        private async Task AnimateVolumeDown(CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            Stopwatch sw = new();

            sw.Start();

            while (sw.Elapsed.TotalSeconds < FadeDuration)
            {
                AnimateVolume = 1f - (float)(sw.Elapsed.TotalSeconds / FadeDuration);

                await Task.Delay(20);

                if (token.IsCancellationRequested)
                    return;
            }

            if (token.IsCancellationRequested)
                return;

            AnimateVolume = 0f;
        }
        private async Task AnimateVolumeUp(CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            Stopwatch sw = new();

            sw.Start();

            while (sw.Elapsed.TotalSeconds < FadeDuration)
            {
                AnimateVolume = (float)(sw.Elapsed.TotalSeconds / FadeDuration);

                await Task.Delay(20);

                if (token.IsCancellationRequested)
                    return;
            }

            if (token.IsCancellationRequested)
                return;

            AnimateVolume = 1f;
        }

        private Task? _currentAnimation;
        private CancellationTokenSource? _cts;
        private void CancelCurrentAnimation()
        {
            if (_currentAnimation is not null && _cts is not null)
            {
                _cts.Cancel();
                _currentAnimation = null;
                _cts = null;
            }
        }
        private void BeginAnimateVolumeDown(Action? onZero = null)
        {
            CancelCurrentAnimation();

            if (!AppSettings.Instance.SmoothPlayPause || FadeDuration <= 0)
            {
                AnimateVolume = 0f;
                onZero?.Invoke();
            }
            else
            {
                _cts = new();

                _currentAnimation = AnimateVolumeDown(_cts.Token).ContinueWith((t) =>
                {
                    onZero?.Invoke();
                });
            }
        }
        private void BeginAnimateVolumeUp(Action? onMax = null)
        {
            CancelCurrentAnimation();

            if (!AppSettings.Instance.SmoothPlayPause || FadeDuration <= 0)
            {
                AnimateVolume = 1f;
                onMax?.Invoke();
            }
            else
            {
                _cts = new();

                _currentAnimation = AnimateVolumeUp(_cts.Token).ContinueWith((t) =>
                {
                    onMax?.Invoke();
                });
            }
        }

        private float _userVolume = 1f;
        private float _replayGainVolume = 1f;
        private float _animateVolume = 1f;
        public float UserVolume
        {
            get => _userVolume;
            set
            {
                _userVolume = value;

                TriggerPropertyChanged(nameof(UserVolume), nameof(TotalVolume));

                UpdateVolume();
            }
        }
        public float ReplayGainVolume
        {
            get => _replayGainVolume;
            set
            {
                _replayGainVolume = value;

                TriggerPropertyChanged(nameof(ReplayGainVolume), nameof(TotalVolume));

                UpdateVolume();
            }
        }
        public float AnimateVolume
        {
            get => _animateVolume;
            set
            {
                _animateVolume = value;

                TriggerPropertyChanged(nameof(AnimateVolume), nameof(TotalVolume));

                UpdateVolume();
            }
        }

        public float TotalVolume
        {
            get
            {
                float total = UserVolume;

                if (AppSettings.Instance.EnableReplayGain)
                    total *= ReplayGainVolume;

                total *= AnimateVolume;

                return total;
            }
        }

        private double _fadeDuration = 0.5;
        public double FadeDuration
        {
            get => _fadeDuration;
            set => ChangeProperty(ref _fadeDuration, value, nameof(FadeDuration));
        }

        public double Position
        {
            get => Bass.BASS_ChannelBytes2Seconds(_handle, Bass.BASS_ChannelGetPosition(_handle));
            set
            {
                Bass.BASS_ChannelSetPosition(_handle, value);

                TriggerPropertyChanged(nameof(Position));
            }
        }
        public double Duration
        {
            get => Bass.BASS_ChannelBytes2Seconds(_handle, Bass.BASS_ChannelGetLength(_handle));
        }

        private readonly float[] _fftBuffer = new float[1024];
        /// <summary>
        /// Updates <paramref name="buffer"/> with current song sample data
        /// </summary>
        private void GetData(float[] buffer)
        {
            if (!_hasInitDefaultDevice || State != BASSActive.BASS_ACTIVE_PLAYING)
                return;

            Bass.BASS_ChannelGetData(_handle, buffer, (int)BASSData.BASS_DATA_FFT1024);
        }

        /// <summary>
        /// Updates <paramref name="peaks"/> with current song peak values based on sample data
        /// </summary>
        public void GetSpectrum(float[] peaks)
        {
            GetData(_fftBuffer);

            int b0 = 0;

            for (int x = 0; x < peaks.Length; x++)
            {
                float peak = 0;

                int b1 = (int)Math.Pow(2, x * 10.0 / (peaks.Length - 1));

                b1 = Math.Clamp(b1, b0 + 1, 1023);

                for (; b0 < b1; b0++)
                {
                    if (peak < _fftBuffer[1 + b0])
                        peak = _fftBuffer[1 + b0];
                }

                int y = (int)(Math.Sqrt(peak) * 3 * 255 - 4);

                y = Math.Clamp(y, 0, 255);

                float yF = y / 255f;

                peaks[x] = yF;
            }
        }

        public static IEnumerable<string> SupportedExtensions => SUPPORTED_EXTENSIONS;
        private static readonly string[] SUPPORTED_EXTENSIONS = new string[]
        {
            ".mp3",
            ".m4a",
            ".wma",
            ".ogg",
            ".opus",
        };
    }
}
