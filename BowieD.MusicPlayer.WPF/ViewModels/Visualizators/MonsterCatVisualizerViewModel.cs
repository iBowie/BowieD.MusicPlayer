using BowieD.MusicPlayer.WPF.Common;
using System;
using System.Collections.Generic;
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


        private readonly List<Rectangle> _visibleRectangles = new();
        private Panel monsterCat_peaksGrid;
        private double _frameRate = 60.0;
        private Color _accentColor = Colors.White;

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

                foreach (var rect in _visibleRectangles)
                {
                    rect.Fill = scb;
                }
            }
        }

        public override void Setup()
        {
            monsterCat_peaksGrid = MainWindowViewModel.View.monsterCat_peaksGrid;

            monsterCat_peaksGrid.Children.Clear();

            for (int i = 0; i < 64; i++)
            {
                var newRect = new Rectangle();

                monsterCat_peaksGrid.Children.Add(newRect);
                _visibleRectangles.Add(newRect);
            }
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

                        var maxH = monsterCat_peaksGrid.ActualHeight;
                        var curH = _visibleRectangles[x].Height;
                        if (double.IsNaN(curH))
                            curH = _visibleRectangles[x].MinHeight;
                        var newH = Math.Clamp(maxH * yF, _visibleRectangles[x].MinHeight, double.PositiveInfinity);

                        // var smoothed = curH + (newH - curH) * 0.75;
                        // var smoothed = smoothStep(curH, newH, 0.75);
                        var smoothed = curH + (newH - curH) * 0.5;
                        // var smoothed = newH;

                        _visibleRectangles[x].Height = smoothed;
                    }
                });
            };
            _visualizerTimer.Start();
        }

        public override void Stop()
        {
            if (_visualizerTimer is not null)
            {
                _visualizerTimer.Stop();
                _visualizerTimer.Dispose();
            }
        }
    }
}
