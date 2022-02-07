using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BowieD.MusicPlayer.WPF.Controls
{
    /// <summary>
    /// Логика взаимодействия для ParticlesControl.xaml
    /// </summary>
    public partial class ParticlesControl : UserControl
    {
        private readonly List<Particle> _particles = new();
        private struct Particle
        {
            public double posX;
            public double posY;
            public readonly double speedX;
            public readonly double speedY;
            public readonly Ellipse ellipse;

            public Particle(double speedX, double speedY, Ellipse ellipse) : this()
            {
                this.speedX = speedX;
                this.speedY = speedY;
                this.ellipse = ellipse;
            }
        }

        public ParticlesControl()
        {
            InitializeComponent();

            mainParticleCanvas.DataContext = this;
        }

        private void OnRender(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                while (_particles.Count < MaxParticles)
                {
                    AddParticle();
                }

                for (int i = _particles.Count - 1; i >= 0; i--)
                {
                    Particle particle = _particles[i];

                    if (particle.posX > ActualWidth || particle.posY > ActualHeight ||
                        particle.posX < 0 || particle.posY < 0)
                    {
                        mainParticleCanvas.Children.Remove(particle.ellipse);

                        _particles.RemoveAt(i);
                    }
                    else
                    {
                        particle.posX += particle.speedX * ParticleSpeedRatio;
                        particle.posY += particle.speedY * ParticleSpeedRatio;

                        Canvas.SetLeft(particle.ellipse, particle.posX);
                        Canvas.SetTop(particle.ellipse, particle.posY);

                        _particles[i] = particle;
                    }
                }
            });
        }

        private Random _random = new();
        private void AddParticle()
        {
            double radius = _random.NextDouble() * 4 + 0.1;
            double opacity = _random.NextDouble() * 0.5 + 0.5;

            Ellipse elps = new()
            {
                Fill = Brushes.White,
                Width = radius,
                Height = radius,
                Opacity = opacity,
            };

            var particle = new Particle(_random.NextDouble() * 5 + 0.5, _random.NextDouble() * 4 - 2, elps);

            particle.posX = _random.NextDouble() * ActualWidth;
            particle.posY = _random.NextDouble() * ActualHeight;

            _particles.Add(particle);

            mainParticleCanvas.Children.Add(elps);
        }

        private Timer? _timer;
        public Timer? Timer
        {
            get => _timer;
            set
            {
                if (_timer is not null)
                {
                    _timer.Elapsed -= OnRender;
                }

                _timer = value;

                if (_timer is not null)
                {
                    _timer.Elapsed += OnRender;
                }
            }
        }

        public double ParticleSpeedRatio
        {
            get { return (double)GetValue(ParticleSpeedRatioProperty); }
            set { SetValue(ParticleSpeedRatioProperty, value); }
        }
        public double ParticleSpeed
        {
            get { return (double)GetValue(ParticleSpeedProperty); }
            set { SetValue(ParticleSpeedProperty, value); }
        }
        public int MaxParticles
        {
            get { return (int)GetValue(MaxParticlesProperty); }
            set { SetValue(MaxParticlesProperty, value); }
        }

        public static readonly DependencyProperty ParticleSpeedRatioProperty =
            DependencyProperty.Register("ParticleSpeedRatio", typeof(double), typeof(ParticlesControl), new PropertyMetadata(1.0));
        public static readonly DependencyProperty ParticleSpeedProperty =
            DependencyProperty.Register("ParticleSpeed", typeof(double), typeof(ParticlesControl), new PropertyMetadata(2.0));
        public static readonly DependencyProperty MaxParticlesProperty =
            DependencyProperty.Register("MaxParticles", typeof(int), typeof(ParticlesControl), new PropertyMetadata((int)32));
    }
}
