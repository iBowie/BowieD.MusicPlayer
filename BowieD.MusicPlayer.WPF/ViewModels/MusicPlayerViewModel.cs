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
using System.Windows.Controls;
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
            _secondTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _fiveSecondTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(5)
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
            _secondTimer.Start();
            _fiveSecondTimer.Start();

            _songQueue.CollectionChanged += _songQueue_CollectionChanged;
        }

        private void _songQueue_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            TriggerPropertyChanged(nameof(UpcomingSong));
        }

        public event TrackChanged? OnTrackChanged;
        public event PlaybackStateChanged? OnPlaybackStateChanged;

        private Un4seen.Bass.BASSActive _prevState = Un4seen.Bass.BASSActive.BASS_ACTIVE_STOPPED;
        private readonly DispatcherTimer _timer, _secondTimer, _fiveSecondTimer;
        private Song _currentSong;
        private ELoopMode _loopMode;
        private EPlayOrigin _playOrigin;
        private readonly ObservableQueue<Song> _userSongQueue = new();
        private readonly ObservableQueue<Song> _songQueue = new();
        private readonly ObservableStack<Song> _songHistory = new();
        private bool _isBigPicture = false;
        private bool _isFullScreen = false;

        public ObservableQueue<Song> UserSongQueue => _userSongQueue;
        public ObservableQueue<Song> SongQueue => _songQueue;
        public ObservableStack<Song> SongHistory => _songHistory;
        public ELoopMode LoopMode
        {
            get => _loopMode;
            set => ChangeProperty(ref _loopMode, value, nameof(LoopMode), nameof(LoopNoneVisible), nameof(LoopQueueVisible), nameof(LoopCurrentVisible));
        }
        public EPlayOrigin PlayOrigin
        {
            get => _playOrigin;
            private set => ChangeProperty(ref _playOrigin, value, nameof(PlayOrigin));
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
                if (UserSongQueue.Count > 0)
                    return UserSongQueue.Peek();

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
        public double Position01
        {
            get => Position / Duration;
            set => Position = value * Duration;
        }

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
            get => LoopMode != ELoopMode.CURRENT && SongQueue.Count > 0 && Duration - Position < 20.0;
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
                var oldSong = CurrentSong;
                var nextSong = DequeueNextSong(out var newOrigin);

                if (newOrigin == EPlayOrigin.NONE)
                    return;

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

        public void NextTrackManual()
        {
            var nextSong = DequeueNextSong(out var newOrigin);

            if (newOrigin == EPlayOrigin.NONE)
                return;

            var cur = CurrentSong;

            if (!cur.IsEmpty)
            {
                if (LoopMode != ELoopMode.NONE)
                {
                    SongQueue.Enqueue(cur);
                }

                SongHistory.Push(cur);
            }

            PlayOrigin = newOrigin;
            CurrentSong = nextSong;
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

        public Song PeekNextSong(out EPlayOrigin origin)
        {
            if (UserSongQueue.Count > 0)
            {
                origin = EPlayOrigin.USER_QUEUE;
                return UserSongQueue.Peek();
            }

            if (SongQueue.Count > 0)
            {
                origin = EPlayOrigin.SOURCE;
                return SongQueue.Peek();
            }

            origin = EPlayOrigin.NONE;
            return Song.EMPTY;
        }

        public Song DequeueNextSong(out EPlayOrigin origin)
        {
            if (UserSongQueue.Count > 0)
            {
                origin = EPlayOrigin.USER_QUEUE;
                return UserSongQueue.Dequeue();
            }

            if (SongQueue.Count > 0)
            {
                origin = EPlayOrigin.SOURCE;
                return SongQueue.Dequeue();
            }

            origin = EPlayOrigin.NONE;
            return Song.EMPTY;
        }

        internal void SetCurrentSong(Song song, bool autoPlay = true)
        {
            CurrentSong = song;

            if (!autoPlay)
                BassFacade.Pause();
        }


        #region Animations

        private static readonly Duration BigPictureSwitchDuration = new Duration(TimeSpan.FromSeconds(0.1));

        private static readonly DoubleAnimation SmallPictureAppearFromLeftAnimation 
            = new(-92.0, 0.0, BigPictureSwitchDuration);
        private static readonly DoubleAnimation SmallPictureDisappearToLeftAnimation 
            = new(0.0, -93.0, BigPictureSwitchDuration);
        private static readonly DoubleAnimation BigPictureOpacityMaskAppearGradientAnimation 
            = new(0.0, 1.0, BigPictureSwitchDuration);
        private static readonly DoubleAnimation BigPictureOpacityMaskDisappearGradientAnimation 
            = new(1.0, 0.0, BigPictureSwitchDuration);
        private static readonly ThicknessAnimation BigPictureMarginAppearGridAnimation 
            = new(new(0, -392, 0, 0), new(0, 0, 0, 0), BigPictureSwitchDuration);
        private static readonly ThicknessAnimation BigPictureMarginDisappearGridAnimation 
            = new(new(0, 0, 0, 0), new(0, -392, 0, 0), BigPictureSwitchDuration);
        private static readonly DoubleAnimation BigPictureRenderTransformAppearAnimation
            = new(392, 0, BigPictureSwitchDuration);
        private static readonly DoubleAnimation BigPictureRenderTransformDisappearAnimation
            = new(0, 392, BigPictureSwitchDuration);

        #endregion

        #region Commands

        private ICommand?
            _nextTrackCommand,
            _loopCommand,
            _shuffleCommand,
            _prevTrackCommand,
            _showBigPictureCommand,
            _collapseBigPictureCommand,
            _enterFullScreenCommand,
            _exitFullScreenCommand,
            _viewQueueCommand,
            _playPauseCommand,
            _clearUserQueueCommand;

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

                        View.smallPictureRenderTranslateTransform.BeginAnimation(TranslateTransform.XProperty, SmallPictureDisappearToLeftAnimation);
                        View.bigPictureGradient1.BeginAnimation(GradientStop.OffsetProperty, BigPictureOpacityMaskAppearGradientAnimation);
                        View.bigPictureGradient2.BeginAnimation(GradientStop.OffsetProperty, BigPictureOpacityMaskAppearGradientAnimation);
                        View.imgBigPicture.BeginAnimation(Grid.MarginProperty, BigPictureMarginAppearGridAnimation);
                        View.bigPictureRenderTTransform.BeginAnimation(TranslateTransform.YProperty, BigPictureRenderTransformAppearAnimation);
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
                        // View.bigPictureGradient1.BeginAnimation(GradientStop.OffsetProperty, BigPictureOpacityMaskDisappearGradientAnimation);
                        // View.bigPictureGradient2.BeginAnimation(GradientStop.OffsetProperty, BigPictureOpacityMaskDisappearGradientAnimation);
                        // View.imgBigPicture.BeginAnimation(Grid.MarginProperty, BigPictureMarginDisappearGridAnimation);
                        // View.bigPictureRenderTTransform.BeginAnimation(TranslateTransform.YProperty, BigPictureRenderTransformDisappearAnimation);
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
        public ICommand ClearUserQueueCommand
        {
            get
            {
                if (_clearUserQueueCommand is null)
                {
                    _clearUserQueueCommand = new BaseCommand(() =>
                    {
                        UserSongQueue.Clear();
                    });
                }

                return _clearUserQueueCommand;
            }
        }

        #endregion

        #region LoopMode

        public bool LoopNoneVisible => LoopMode == ELoopMode.NONE;
        public bool LoopCurrentVisible => LoopMode == ELoopMode.CURRENT;
        public bool LoopQueueVisible => LoopMode == ELoopMode.QUEUE;

        #endregion

        #region System Media Transport Controls
        #if WINDOWS10_0_19041_0_OR_GREATER
        private Windows.Media.SystemMediaTransportControls? _systemMediaTransportControls;

        public void SetupMediaTransport()
        {
            if (_systemMediaTransportControls is not null)
                return;

            _systemMediaTransportControls = View.GetSystemMediaTransportControls();

            _systemMediaTransportControls.IsRecordEnabled = false;
            _systemMediaTransportControls.IsRewindEnabled = false;
            _systemMediaTransportControls.IsStopEnabled = false;
            _systemMediaTransportControls.PlaybackRate = 1.0;

            _systemMediaTransportControls.ButtonPressed += _systemMediaTransportControls_ButtonPressed;

            OnTrackChanged += async (song) =>
            {
                var updater = _systemMediaTransportControls.DisplayUpdater;

                if (song.IsEmpty || song.PictureData is null)
                {
                    updater.ClearAll();

                    updater.Update();
                }
                else
                {
                    updater.Type = Windows.Media.MediaPlaybackType.Music;

                    using (var inMemory = new Windows.Storage.Streams.InMemoryRandomAccessStream())
                    {
                        await System.IO.WindowsRuntimeStreamExtensions.AsStreamForWrite(inMemory).WriteAsync(song.PictureData, 0, song.PictureData.Length);

                        await inMemory.FlushAsync();

                        inMemory.Seek(0);

                        updater.Thumbnail = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromStream(inMemory);

                        updater.MusicProperties.Artist = song.Artist;
                        updater.MusicProperties.Title = song.Title;
                        updater.MusicProperties.AlbumTitle = song.Album;

                        updater.Update();
                    }
                }
            };

            OnTrackChanged?.Invoke(CurrentSong);

            OnPlaybackStateChanged += (song, oldState, newState) =>
            {
                _systemMediaTransportControls.PlaybackStatus = BassFacade.State switch
                {
                    Un4seen.Bass.BASSActive.BASS_ACTIVE_STOPPED => Windows.Media.MediaPlaybackStatus.Stopped,
                    Un4seen.Bass.BASSActive.BASS_ACTIVE_STALLED => Windows.Media.MediaPlaybackStatus.Changing,
                    Un4seen.Bass.BASSActive.BASS_ACTIVE_PAUSED => Windows.Media.MediaPlaybackStatus.Paused,
                    Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING => Windows.Media.MediaPlaybackStatus.Playing,
                    _ => throw new Exception(),
                };

                var timeline = new Windows.Media.SystemMediaTransportControlsTimelineProperties();

                timeline.StartTime = TimeSpan.FromSeconds(0);
                timeline.EndTime = TimeSpan.FromSeconds(Duration);
                timeline.MinSeekTime = TimeSpan.FromSeconds(0);
                timeline.MaxSeekTime = TimeSpan.FromSeconds(Duration);
                timeline.Position = TimeSpan.FromSeconds(Position);

                _systemMediaTransportControls.UpdateTimelineProperties(timeline);
            };

            _timer.Tick += (sender, e) =>
            {
                UpdateMediaTransport();
            };
        }

        private void _systemMediaTransportControls_ButtonPressed(Windows.Media.SystemMediaTransportControls sender, Windows.Media.SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case Windows.Media.SystemMediaTransportControlsButton.Play:
                case Windows.Media.SystemMediaTransportControlsButton.Pause:
                    Dispatcher.Invoke(() =>
                    {
                        PlayPauseCommand.Execute(null);
                    });
                    break;
                case Windows.Media.SystemMediaTransportControlsButton.Next:
                    Dispatcher.Invoke(() =>
                    {
                        NextTrackCommand.Execute(null);
                    });
                    break;
                case Windows.Media.SystemMediaTransportControlsButton.Previous:
                    Dispatcher.Invoke(() =>
                    {
                        PrevTrackCommand.Execute(null);
                    });
                    break;
            }
        }

        public void UpdateMediaTransport()
        {
            if (_systemMediaTransportControls is not null)
            {
                _systemMediaTransportControls.IsPauseEnabled = IsPauseButton;
                _systemMediaTransportControls.IsPlayEnabled = !IsPauseButton;
                _systemMediaTransportControls.IsNextEnabled = !PeekNextSong(out _).IsEmpty;
                _systemMediaTransportControls.IsPreviousEnabled = SongHistory.Count > 0;
            }
        }
        #endif
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
    public enum EPlayOrigin : byte
    {
        NONE,
        USER_QUEUE,
        SOURCE
    }
}
