using BowieD.MusicPlayer.WPF.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BowieD.MusicPlayer.WPF.ViewModels.Visualizators
{
    public sealed class MonsterCatVisualizerViewModel : VisualizerViewModelBase
    {
        public MonsterCatVisualizerViewModel(Panel boundPanel, MainWindowViewModel mainWindowViewModel) : base(boundPanel, mainWindowViewModel)
        {
        }

        public override string VisualizerName => "Monstercat Classic Visualizer";

        private float[] peaksData;
        private readonly List<Rectangle> _visibleRectangles = new();
        private Panel monsterCat_peaksGrid;
        private double _frameRate = 60.0;
        private double _particles = 64;
        private Color _accentColor = Colors.White;
        private int _barCount;
        private bool _autoPickColor = false;
        private double _smoothFactor = 5.0;

        public double FrameRate
        {
            get => _frameRate;
            set
            {
                ChangeProperty(ref _frameRate, value, nameof(FrameRate));

                if (_visualizerTimer is not null)
                    _visualizerTimer.Interval = 1000.0 / FrameRate;
            }
        }
        public Color AccentColor
        {
            get => _accentColor;
            set
            {
                ChangeProperty(ref _accentColor, value, nameof(AccentColor));

                SolidColorBrush scb = new(value);

                for (int i = 0; i < _visibleRectangles.Count; i++)
                {
                    Rectangle? rect = _visibleRectangles[i];
                    rect.Fill = scb;
                }
            }
        }
        public double MaxParticles
        {
            get => _particles;
            set => ChangeProperty(ref _particles, value, nameof(MaxParticles));
        }
        public double SpeedRatio { get; private set; } = 1.0;
        public int BarCount
        {
            get => _barCount;
            set
            {
                ChangeProperty(ref _barCount, value, nameof(BarCount));

                SolidColorBrush scb = new(AccentColor);

                peaksData = new float[value];

                _visibleRectangles.Clear();
                monsterCat_peaksGrid.Children.Clear();

                for (int i = 0; i < value; i++)
                {
                    var newRect = new Rectangle()
                    {
                        Fill = scb,
                    };

                    monsterCat_peaksGrid.Children.Add(newRect);
                    _visibleRectangles.Add(newRect);
                }
            }
        }
        public bool AutoPickColor
        {
            get => _autoPickColor;
            set
            {
                ChangeProperty(ref _autoPickColor, value, nameof(AutoPickColor));

                if (value)
                {
                    MusicPlayerViewModel_OnTrackChanged(MainWindowViewModel.View.MusicPlayerViewModel.CurrentSong);
                }
            }
        }
        public double SmoothFactor
        {
            get => _smoothFactor;
            set => ChangeProperty(ref _smoothFactor, value, nameof(SmoothFactor));
        }

        public override void Setup()
        {
            monsterCat_peaksGrid = MainWindowViewModel.View.monsterCat_peaksGrid;

            BarCount = 64;
            AccentColor = Colors.White;
        }

        private Timer? _visualizerTimer;

        public override void Start()
        {
            _visualizerTimer = new();
            _visualizerTimer.Interval = 1000.0 / FrameRate; // 60 times per second by default
            _visualizerTimer.AutoReset = true;
            _visualizerTimer.Elapsed += (sender, e) =>
            {
                MainWindowViewModel.Dispatcher.Invoke(() =>
                {
                    BassFacade.GetSpectrum(peaksData);

                    for (int i = 0; i < peaksData.Length; i++)
                    {
                        var yF = peaksData[i];

                        var maxH = monsterCat_peaksGrid.ActualHeight;
                        var curH = _visibleRectangles[i].Height;
                        if (double.IsNaN(curH))
                            curH = _visibleRectangles[i].MinHeight;
                        var newH = Math.Clamp(maxH * yF, _visibleRectangles[i].MinHeight, double.PositiveInfinity);

                        // var smoothed = curH + (newH - curH) * 0.75;
                        // var smoothed = smoothStep(curH, newH, 0.75);
                        // var smoothed = curH + (newH - curH) * 0.5;
                        // var smoothed = newH;

                        double smoothed;

                        if (SmoothFactor < 1)
                        {
                            smoothed = newH;
                        }
                        else
                        {
                            if (curH < newH)
                            {
                                smoothed = curH + ((newH - curH) / SmoothFactor);
                            }
                            else if (curH > newH)
                            {
                                smoothed = curH - ((curH - newH) / SmoothFactor);
                            }
                            else
                            {
                                smoothed = newH;
                            }
                        }

                        _visibleRectangles[i].Height = smoothed;

                        SpeedRatio = peaksData.Average();
                        TriggerPropertyChanged(nameof(SpeedRatio));
                    }
                });
            };
            _visualizerTimer.Start();

            MainWindowViewModel.View.monsterCat_particles.Timer = _visualizerTimer;

            MusicPlayerViewModel.OnTrackChanged += MusicPlayerViewModel_OnTrackChanged;
            MusicPlayerViewModel_OnTrackChanged(MainWindowViewModel.View.MusicPlayerViewModel.CurrentSong);
        }

        private void MusicPlayerViewModel_OnTrackChanged(Models.Song newSong)
        {
            if (AutoPickColor)
            {
                var newAccent = VibrantColor.GetVibrantColor(newSong.PictureData);

                AccentColor = newAccent;
            }
        }

        public override void Stop()
        {
            MusicPlayerViewModel.OnTrackChanged -= MusicPlayerViewModel_OnTrackChanged;

            if (_visualizerTimer is not null)
            {
                _visualizerTimer.Stop();
                _visualizerTimer.Dispose();
                _visualizerTimer = null;
            }

            MainWindowViewModel.View.monsterCat_particles.Timer = _visualizerTimer;
        }
    }
}
