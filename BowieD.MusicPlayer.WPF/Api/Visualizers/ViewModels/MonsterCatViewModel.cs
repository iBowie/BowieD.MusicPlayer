using BowieD.MusicPlayer.WPF.Api.Visualizers.Impl;
using BowieD.MusicPlayer.WPF.Api.Visualizers.Views;
using BowieD.MusicPlayer.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace BowieD.MusicPlayer.WPF.Api.Visualizers.ViewModels
{
    public sealed class MonsterCatViewModel : VisualizerViewModelBase<MonsterCatPage, MonsterCatVisualizer>
    {
        public MonsterCatViewModel(MonsterCatPage page, MonsterCatVisualizer visualizer, MainWindowViewModel mainWindowViewModel, MusicPlayerViewModel musicPlayerViewModel) : base(page, visualizer, mainWindowViewModel, musicPlayerViewModel)
        {
            monsterCat_peaksGrid = page.monsterCat_peaksGrid;

            BarCount = 64;
        }

        private float[] peaksData = Array.Empty<float>();
        private readonly List<Rectangle> _visibleRectangles = new();
        private Panel monsterCat_peaksGrid;
        private double _frameRate = 60.0;
        private double _particles = 64;
        private int _barCount;
        private double _smoothFactor = 5.0;
        private int _hideBarsFromRight = 8;

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

                peaksData = new float[value];

                _visibleRectangles.Clear();
                monsterCat_peaksGrid.Children.Clear();

                for (int i = 0, j = value - 1; i < value; i++, j--)
                {
                    var newRect = new Rectangle();

                    if (j > HideBarsFromRight)
                    {
                        newRect.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        newRect.Visibility = System.Windows.Visibility.Collapsed;
                    }

                    monsterCat_peaksGrid.Children.Add(newRect);
                    _visibleRectangles.Add(newRect);
                }
            }
        }
        public double SmoothFactor
        {
            get => _smoothFactor;
            set => ChangeProperty(ref _smoothFactor, value, nameof(SmoothFactor));
        }
        public int HideBarsFromRight
        {
            get => _hideBarsFromRight;
            set
            {
                ChangeProperty(ref _hideBarsFromRight, value, nameof(HideBarsFromRight));

                for (int i = 0, j = BarCount - 1; i < BarCount; i++, j--)
                {
                    var bar = monsterCat_peaksGrid.Children[i];

                    if (j > value)
                    {
                        bar.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        bar.Visibility = System.Windows.Visibility.Collapsed;
                    }
                }
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
                MainWindowViewModel.Dispatcher.Invoke(() =>
                {
                    MainWindowViewModel.View.MusicPlayerViewModel.BassWrapper.GetSpectrum(peaksData);

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

            Page.monsterCat_particles.Timer = _visualizerTimer;
        }

        public override void Stop()
        {
            if (_visualizerTimer is not null)
            {
                _visualizerTimer.Stop();
                _visualizerTimer.Dispose();
                _visualizerTimer = null;
            }

            Page.monsterCat_particles.Timer = _visualizerTimer;
        }
    }
}
