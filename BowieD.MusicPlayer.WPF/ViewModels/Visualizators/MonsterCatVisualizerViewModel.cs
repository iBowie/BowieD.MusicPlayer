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
        public MonsterCatVisualizerViewModel(MainWindowViewModel mainWindowViewModel) : base(mainWindowViewModel)
        {
        }

        private float[] peaksData;
        private readonly List<Rectangle> _visibleRectangles = new();
        private Panel monsterCat_peaksGrid;
        private double _frameRate = 60.0;
        private double _particles;
        private Color _accentColor = Colors.White;
        private int _barCount;

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
                BassFacade.GetSpectrum(_fft);

                MainWindowViewModel.Dispatcher.Invoke(() =>
                {
                    int b0 = 0;
                    int y = 0;

                    for (int x = 0; x < _visibleRectangles.Count; x++)
                    {
                        float peak = 0;
                        int b1 = (int)Math.Pow(2, x * 10.0 / (_visibleRectangles.Count - 1));
                        if (b1 > 1023) b1 = 1023;
                        if (b1 <= b0) b1 = b0 + 1;
                        for (; b0 < b1; b0++)
                        {
                            if (peak < _fft[1 + b0]) peak = _fft[1 + b0];
                        }
                        y = (int)(Math.Sqrt(peak) * 3 * 255 - 4);
                        if (y > 255) y = 255;
                        if (y < 0) y = 0;

                        float yF = y / 255f;

                        peaksData[x] = yF;

                        var maxH = monsterCat_peaksGrid.ActualHeight;
                        var curH = _visibleRectangles[x].Height;
                        if (double.IsNaN(curH))
                            curH = _visibleRectangles[x].MinHeight;
                        var newH = Math.Clamp(maxH * yF, _visibleRectangles[x].MinHeight, double.PositiveInfinity);

                        // var smoothed = curH + (newH - curH) * 0.75;
                        // var smoothed = smoothStep(curH, newH, 0.75);
                        // var smoothed = curH + (newH - curH) * 0.5;
                        // var smoothed = newH;

                        double smoothed;
                        const double smoothFactor = 5.0;

                        if (curH < newH)
                        {
                            smoothed = curH + ((newH - curH) / smoothFactor);
                        }
                        else if (curH > newH)
                        {
                            smoothed = curH - ((curH - newH) / smoothFactor);
                        }
                        else
                        {
                            smoothed = newH;
                        }

                        _visibleRectangles[x].Height = smoothed;

                        SpeedRatio = peaksData.Average();
                        TriggerPropertyChanged(nameof(SpeedRatio));
                    }
                });
            };
            _visualizerTimer.Start();

            MainWindowViewModel.View.monsterCat_particles.Timer = _visualizerTimer;
        }

        public override void Stop()
        {
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
