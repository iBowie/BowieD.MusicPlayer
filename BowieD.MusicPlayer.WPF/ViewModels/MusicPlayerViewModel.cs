using BowieD.MusicPlayer.WPF.Collections;
using BowieD.MusicPlayer.WPF.Common;
using BowieD.MusicPlayer.WPF.Extensions;
using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using System.Windows.Threading;

namespace BowieD.MusicPlayer.WPF.ViewModels
{
    public delegate void PlaybackStateChanged(Song song, Un4seen.Bass.BASSActive newState, Un4seen.Bass.BASSActive oldState);
    public delegate void TrackChanged(Song newSong);

    public sealed class MusicPlayerViewModel : BaseViewModelView<MainWindow>
    {
        public MusicPlayerViewModel(MainWindow view) : base(view)
        {
            BassFacade.Init();

            _timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };

            _timer.Tick += (sender, e) =>
            {
                Un4seen.Bass.BASSActive newState = BassFacade.State;

                if (newState != _prevState)
                {
                    OnPlaybackStateChanged?.Invoke(CurrentSong, newState, _prevState);
                }

                _prevState = newState;

                TriggerPropertyChanged(nameof(Position), nameof(Position01), nameof(Duration), nameof(IsUpcomingSongVisible), nameof(IsPauseButton));
                View.ViewModel.TriggerPropertyChanged(nameof(MainWindowViewModel.WindowTitle));

                if (CurrentSong.IsEmpty || newState == Un4seen.Bass.BASSActive.BASS_ACTIVE_STOPPED)
                {
                    NextTrackAuto();
                }
            };

            OnPlaybackStateChanged += (song, oldState, newState) =>
            {
                TriggerPropertyChanged(nameof(ProgressState));
            };

            _timer.Start();

            _songQueue.CollectionChanged += _songQueue_CollectionChanged;
        }

        private void _songQueue_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            TriggerPropertyChanged(nameof(UpcomingSong));
        }

        public event TrackChanged OnTrackChanged;
        public event PlaybackStateChanged OnPlaybackStateChanged;

        private Un4seen.Bass.BASSActive _prevState = Un4seen.Bass.BASSActive.BASS_ACTIVE_STOPPED;
        private DispatcherTimer _timer;
        private Song _currentSong;
        private ELoopMode _loopMode;
        private readonly ObservableQueue<Song> _songQueue = new();
        private readonly ObservableStack<Song> _songHistory = new();
        private bool _isBigPicture = false;
        private bool _isFullScreen = false;

        public ObservableQueue<Song> SongQueue => _songQueue;
        public ObservableStack<Song> SongHistory => _songHistory;
        public ELoopMode LoopMode
        {
            get => _loopMode;
            set => ChangeProperty(ref _loopMode, value, nameof(LoopMode), nameof(LoopNoneVisible), nameof(LoopQueueVisible), nameof(LoopCurrentVisible));
        }
        public Song CurrentSong
        {
            get => _currentSong;
            private set
            {
                ChangeProperty(ref _currentSong, value, nameof(CurrentSong));

                if (!value.IsEmpty && value.IsAvailable)
                {
                    BassFacade.Play(value.FileName);

                    OnTrackChanged?.Invoke(value);
                }
                else
                {
                    if (!BassFacade.IsStopped)
                        BassFacade.Stop();
                }
            }
        }
        public Song UpcomingSong
        {
            get
            {
                if (SongQueue.Count > 0)
                    return SongQueue.Peek();

                return Song.EMPTY;
            }
        }
        public double Position
        {
            get => BassFacade.GetStreamPositionInSeconds();
            set => BassFacade.SetStreamPositionInSeconds(value);
        }
        public double Duration
        {
            get => BassFacade.GetStreamLengthInSeconds();
        }
        public double Volume
        {
            get { return (double)GetValue(VolumeProperty); }
            set { SetValue(VolumeProperty, value); }
        }
        public bool IsBigPicture
        {
            get => _isBigPicture;
            set => ChangeProperty(ref _isBigPicture, value, nameof(IsBigPicture));
        }
        public bool IsPauseButton
        {
            get
            {
                if (BassFacade.State == Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING)
                    return true;

                return false;
            }
        }
        public double Position01 => Position / Duration;
        public TaskbarItemProgressState ProgressState
        {
            get
            {
                return BassFacade.State switch
                {
                    Un4seen.Bass.BASSActive.BASS_ACTIVE_STOPPED => TaskbarItemProgressState.None,
                    Un4seen.Bass.BASSActive.BASS_ACTIVE_STALLED => TaskbarItemProgressState.Error,
                    Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING => TaskbarItemProgressState.Normal,
                    Un4seen.Bass.BASSActive.BASS_ACTIVE_PAUSED => TaskbarItemProgressState.Paused,
                    _ => throw new Exception(),
                };
            }
        }
        public bool IsFullScreen
        {
            get => _isFullScreen;
            set => ChangeProperty(ref _isFullScreen, value, nameof(IsFullScreen));
        }
        public bool IsUpcomingSongVisible
        {
            get => SongQueue.Count > 0 && Duration - Position < 20.0;
        }

        public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register("Volume", typeof(double), typeof(MusicPlayerViewModel), new PropertyMetadata(100.0, VolumeChangedCallback));

        private static void VolumeChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is double newValue)
            {
                BassFacade.SetVolume(newValue);
            }
        }

        private void Clean()
        {
            CurrentSong = Song.EMPTY;
            _songQueue.Clear();
            _songHistory.Clear();
        }

        public void PlayPlaylist(Playlist playlist, bool shuffle = false)
        {
            Clean();

            List<Song> safeSongs = new(playlist.Songs);

            if (shuffle)
                safeSongs.Shuffle();

            foreach (var s in safeSongs)
                _songQueue.Enqueue(s);
        }
        public void PlaySongFromPlaylist(Song song, Playlist playlist, bool shuffle = false)
        {
            Clean();

            var index = playlist.Songs.IndexOf(song);

            if (index == -1)
                return;

            _songQueue.Enqueue(song);

            for (int i = index + 1; i < playlist.Songs.Count; i++)
            {
                _songQueue.Enqueue(playlist.Songs[i]);
            }

            for (int i = 0; i < index; i++)
            {
                _songQueue.Enqueue(playlist.Songs[i]);
            }
        }

        public void NextTrackAuto()
        {
            if (LoopMode == ELoopMode.CURRENT && !CurrentSong.IsEmpty)
            {
                CurrentSong = CurrentSong;
            }
            else
            {
                if (SongQueue.Count > 0)
                {
                    var oldSong = CurrentSong;
                    var nextSong = SongQueue.Dequeue();

                    if (!oldSong.IsEmpty)
                    {
                        if (LoopMode == ELoopMode.QUEUE)
                        {
                            SongQueue.Enqueue(oldSong);
                        }

                        SongHistory.Push(oldSong);
                    }

                    CurrentSong = nextSong;
                }
            }
        }

        public void NextTrackManual()
        {
            if (SongQueue.Count > 0)
            {
                var nextSong = SongQueue.Dequeue();

                var cur = CurrentSong;

                if (!cur.IsEmpty)
                {
                    if (LoopMode != ELoopMode.NONE)
                    {
                        SongQueue.Enqueue(cur);
                    }
                }

                CurrentSong = nextSong;
            }
        }

        public void PrevTrack()
        {
            if (SongHistory.Count > 0)
            {
                var songFromHistory = SongHistory.Pop();

                var cur = CurrentSong;

                if (!cur.IsEmpty)
                    SongQueue.Insert(0, cur);

                CurrentSong = songFromHistory;
            }
        }

        #region Animations

        private static readonly DoubleAnimation SmallPictureAppearFromLeftAnimation = new(-92.0, 0.0, new Duration(TimeSpan.FromSeconds(0.1)));
        private static readonly DoubleAnimation SmallPictureDisappearToLeftAnimation = new(0.0, -93.0, new Duration(TimeSpan.FromSeconds(0.1)));

        #endregion

        #region Commands

        private ICommand
            _nextTrackCommand,
            _loopCommand,
            _shuffleCommand,
            _prevTrackCommand,
            _showBigPictureCommand,
            _collapseBigPictureCommand,
            _enterFullScreenCommand,
            _exitFullScreenCommand,
            _viewQueueCommand,
            _playPauseCommand;

        public ICommand PlayPauseCommand
        {
            get
            {
                if (_playPauseCommand is null)
                {
                    _playPauseCommand = new BaseCommand(() =>
                    {
                        switch (BassFacade.State)
                        {
                            case Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING:
                                BassFacade.Pause();
                                break;
                            case Un4seen.Bass.BASSActive.BASS_ACTIVE_PAUSED:
                                BassFacade.Resume();
                                break;
                        }
                    }, () =>
                    {
                        return BassFacade.State == Un4seen.Bass.BASSActive.BASS_ACTIVE_PAUSED ||
                               BassFacade.State == Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING;
                    });
                }

                return _playPauseCommand;
            }
        }
        public ICommand NextTrackCommand
        {
            get
            {
                if (_nextTrackCommand is null)
                {
                    _nextTrackCommand = new BaseCommand(() =>
                    {
                        NextTrackManual();
                    }, () =>
                    {
                        return SongQueue.Count > 0;
                    });
                }

                return _nextTrackCommand;
            }
        }
        public ICommand PrevTrackCommand
        {
            get
            {
                if (_prevTrackCommand is null)
                {
                    _prevTrackCommand = new BaseCommand(() =>
                    {
                        PrevTrack();
                    }, () =>
                    {
                        return SongHistory.Count > 0;
                    });
                }

                return _prevTrackCommand;
            }
        }
        public ICommand ShuffleCommand
        {
            get
            {
                if (_shuffleCommand is null)
                {
                    _shuffleCommand = new BaseCommand(() =>
                    {
                        SongQueue.Shuffle();
                    }, () =>
                    {
                        return SongQueue.Count > 0;
                    });
                }

                return _shuffleCommand;
            }
        }
        public ICommand LoopCommand
        {
            get
            {
                if (_loopCommand is null)
                {
                    _loopCommand = new BaseCommand(() =>
                    {
                        LoopMode = LoopMode switch
                        {
                            ELoopMode.NONE => ELoopMode.QUEUE,
                            ELoopMode.QUEUE => ELoopMode.CURRENT,
                            ELoopMode.CURRENT => ELoopMode.NONE,
                            _ => throw new Exception(),
                        };
                    });
                }

                return _loopCommand;
            }
        }
        public ICommand ShowBigPictureCommand
        {
            get
            {
                if (_showBigPictureCommand is null)
                {
                    _showBigPictureCommand = new BaseCommand(() =>
                    {
                        IsBigPicture = true;

                        View.smallPictureRenderTranslateTransform.BeginAnimation(TranslateTransform.XProperty, SmallPictureDisappearToLeftAnimation); ;
                    },
                    () => !IsBigPicture);
                }

                return _showBigPictureCommand;
            }
        }
        public ICommand CollapseBigPictureCommand
        {
            get
            {
                if (_collapseBigPictureCommand is null)
                {
                    _collapseBigPictureCommand = new BaseCommand(() =>
                    {
                        IsBigPicture = false;

                        View.smallPictureRenderTranslateTransform.BeginAnimation(TranslateTransform.XProperty, SmallPictureAppearFromLeftAnimation);
                    }, () => IsBigPicture);
                }

                return _collapseBigPictureCommand;
            }
        }
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

                        View.fullScreenViewGrid.Visibility = Visibility.Visible;
                        View.normalViewGrid.Visibility = Visibility.Collapsed;

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
                        View.WindowStyle = WindowStyle.SingleBorderWindow;
                        View.WindowState = WindowState.Normal;
                        View.ResizeMode = ResizeMode.CanResize;
                        View.UseNoneWindowStyle = false;
                        View.ShowTitleBar = true;
                        View.IgnoreTaskbarOnMaximize = false;

                        View.fullScreenViewGrid.Visibility = Visibility.Collapsed;
                        View.normalViewGrid.Visibility = Visibility.Visible;

                        IsFullScreen = false;
                    }, () =>
                    {
                        return IsFullScreen;
                    });
                }

                return _exitFullScreenCommand;
            }
        }
        public ICommand ViewQueueCommand
        {
            get
            {
                if (_viewQueueCommand is null)
                {
                    _viewQueueCommand = new GenericCommand<Popup>((p) =>
                    {
                        if (p is null)
                            return;

                        p.IsOpen = !p.IsOpen;
                    });
                }

                return _viewQueueCommand;
            }
        }

        #endregion

        #region LoopMode

        public bool LoopNoneVisible => LoopMode == ELoopMode.NONE;
        public bool LoopCurrentVisible => LoopMode == ELoopMode.CURRENT;
        public bool LoopQueueVisible => LoopMode == ELoopMode.QUEUE;

        #endregion
    }
    public enum ELoopMode : byte
    {
        [Description("None")]
        NONE,
        [Description("One Song")]
        CURRENT,
        [Description("Queue")]
        QUEUE
    }
}
