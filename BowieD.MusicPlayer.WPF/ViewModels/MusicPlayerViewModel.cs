using BowieD.MusicPlayer.WPF.Collections;
using BowieD.MusicPlayer.WPF.Common;
using BowieD.MusicPlayer.WPF.Extensions;
using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.Views;
using BowieD.MusicPlayer.WPF.Views.Pages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
                Interval = TimeSpan.FromMilliseconds(1000.0 / 40.0) // 40 times per second should be enough
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

                TriggerPropertyChanged(nameof(Position), nameof(Position01), nameof(Duration), nameof(IsUpcomingSongVisible), nameof(IsPauseButton), nameof(UpcomingSongSlider));
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

            SongQueue.CollectionChanged += _songQueue_CollectionChanged;
            UserSongQueue.CollectionChanged += _userSongQueue_CollectionChanged;
        }

        private void _userSongQueue_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            TriggerPropertyChanged(nameof(IsUserQueueVisible));
        }

        private void _songQueue_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            TriggerPropertyChanged(nameof(UpcomingSong));
        }

        public static event TrackChanged? OnTrackChanged;
        public event PlaybackStateChanged? OnPlaybackStateChanged;

        private Un4seen.Bass.BASSActive _prevState = Un4seen.Bass.BASSActive.BASS_ACTIVE_STOPPED;
        private readonly DispatcherTimer _timer, _secondTimer, _fiveSecondTimer;
        private Song _currentSong;
        private ELoopMode _loopMode;
        private bool _isShuffleEnabled = false;
        private EPlayOrigin _playOrigin;
        private bool _isBigPicture = false;
        private bool _isFullScreen = false;
        private ISongSource? _currentSongSource;

        public ObservableLIFOFIFO<Song> UserSongQueue { get; } = new();
        public ObservableLIFOFIFO<Song> SongQueue { get; } = new();
        public ObservableLIFOFIFO<Song> SongHistory { get; } = new();

        public IEnumerable<Song> AllActiveSongs
        {
            get
            {
                yield return CurrentSong;

                foreach (var s in UserSongQueue.Concat(SongQueue).Concat(SongHistory))
                    yield return s;
            }
        }

        public ELoopMode LoopMode
        {
            get => _loopMode;
            set => ChangeProperty(ref _loopMode, value, nameof(LoopMode), nameof(LoopNoneVisible), nameof(LoopQueueVisible), nameof(LoopCurrentVisible));
        }
        public bool IsShuffleEnabled
        {
            get => _isShuffleEnabled;
            set => ChangeProperty(ref _isShuffleEnabled, value, nameof(IsShuffleEnabled));
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
                return PeekNextSong(out _);
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
        public bool LockAuto { get; set; } = false;

        public ISongSource? CurrentSongSource
        {
            get => _currentSongSource;
            set
            {
                ChangeProperty(ref _currentSongSource, value, nameof(CurrentSongSource));

                if (value is not null)
                {
                    SongQueue.Clear();

                    IEnumerable<Song> toAdd;

                    var sSrc = value.GetSongs(CurrentSong);

                    if (IsShuffleEnabled)
                        toAdd = sSrc.ShuffleLinq();
                    else
                        toAdd = sSrc;

                    foreach (var s in toAdd)
                    {
                        SongQueue.EnqueueFIFO(s);
                    }
                }
            }
        }

        public const double UPCOMING_SONG_PRETIME = 20.0;

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
            get => LoopMode != ELoopMode.CURRENT && SongQueue.Count > 0 && Duration - Position < UPCOMING_SONG_PRETIME;
        }
        public double UpcomingSongSlider
        {
            get
            {
                double curPos = Position;
                double curDur = Duration;
                double preTime = UPCOMING_SONG_PRETIME;

                return Math.Clamp(1.0 - ((curDur - curPos) / preTime), 0, 1);
            }
        }
        public bool IsUserQueueVisible => UserSongQueue.Count > 0;

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
            CurrentSongSource = null;
            CurrentSong = Song.EMPTY;
            SongQueue.Clear();
            SongHistory.Clear();
        }

        public void PlayPlaylist(Playlist playlist, bool shuffle = false)
        {
            Clean();

            if (shuffle)
                IsShuffleEnabled = true;

            CurrentSongSource = playlist;
        }
        public void PlaySongFromPlaylist(Song song, Playlist playlist, bool shuffle = false)
        {
            var index = playlist.Songs.IndexOf(song);

            if (index == -1)
                return;

            Clean();

            CurrentSong = song;
            CurrentSongSource = playlist;

            if (shuffle)
                IsShuffleEnabled = true;
        }

        public void NextTrackAuto()
        {
            if (LockAuto)
                return;

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
                        SongQueue.EnqueueFIFO(oldSong);
                    }

                    SongHistory.PushLIFO(oldSong);
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
                    SongQueue.EnqueueFIFO(cur);
                }

                SongHistory.PushLIFO(cur);
            }

            PlayOrigin = newOrigin;
            CurrentSong = nextSong;
        }

        public void PrevTrack()
        {
            var next = PopPrevSong(out var origin);

            if (next.IsEmpty)
                return;

            var cur = CurrentSong;

            if (!cur.IsEmpty)
                SongQueue.PushLIFO(cur);

            this.PlayOrigin = origin;

            CurrentSong = next;
        }

        public Song PeekNextSong(out EPlayOrigin origin)
        {
            if (UserSongQueue.Count > 0)
            {
                origin = EPlayOrigin.USER_QUEUE;
                return UserSongQueue.PeekFIFO();
            }

            if (SongQueue.Count > 0)
            {
                origin = EPlayOrigin.SOURCE;
                return SongQueue.PeekFIFO();
            }

            origin = EPlayOrigin.NONE;
            return Song.EMPTY;
        }

        public Song DequeueNextSong(out EPlayOrigin origin)
        {
            if (UserSongQueue.Count > 0)
            {
                origin = EPlayOrigin.USER_QUEUE;
                return UserSongQueue.DequeueFIFO();
            }

            if (SongQueue.Count > 0)
            {
                origin = EPlayOrigin.SOURCE;
                return SongQueue.DequeueFIFO();
            }

            origin = EPlayOrigin.NONE;
            return Song.EMPTY;
        }

        public Song PeekPrevSong(out EPlayOrigin origin)
        {
            if (SongHistory.Count > 0)
            {
                origin = EPlayOrigin.USER_QUEUE;
                return SongHistory.PeekLIFO();
            }

            if (SongQueue.Count > 0)
            {
                origin = EPlayOrigin.SOURCE;
                return SongQueue.PeekLIFO();
            }

            origin = EPlayOrigin.NONE;
            return Song.EMPTY;
        }

        public Song PopPrevSong(out EPlayOrigin origin)
        {
            if (SongHistory.Count > 0)
            {
                origin = EPlayOrigin.USER_QUEUE;
                return SongHistory.PopLIFO();
            }

            if (SongQueue.Count > 0)
            {
                origin = EPlayOrigin.SOURCE;
                return SongQueue.PopLIFO();
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
                            case Un4seen.Bass.BASSActive.BASS_ACTIVE_STOPPED:
                                BassFacade.Play(CurrentSong.FileName);
                                break;
                        }
                    }, () =>
                    {
                        return BassFacade.State == Un4seen.Bass.BASSActive.BASS_ACTIVE_PAUSED ||
                               BassFacade.State == Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING ||
                               (BassFacade.State == Un4seen.Bass.BASSActive.BASS_ACTIVE_STOPPED && !CurrentSong.IsEmpty && CurrentSong.IsAvailable);
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
                        return !UpcomingSong.IsEmpty;
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
                        if (Position >= 15)
                        {
                            Position = 0;
                        }
                        else
                        {
                            PrevTrack();
                        }
                    }, () =>
                    {
                        return (BassFacade.State == Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING && Position >= 15) || !PeekPrevSong(out _).IsEmpty;
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
                        IsShuffleEnabled = !IsShuffleEnabled;

                        if (CurrentSongSource is not null)
                        {
                            SongQueue.Clear();

                            IEnumerable<Song> toAdd;

                            var sSrc = CurrentSongSource.GetSongs(CurrentSong);

                            if (IsShuffleEnabled)
                                toAdd = sSrc.ShuffleLinq();
                            else
                                toAdd = sSrc;

                            foreach (var s in toAdd)
                            {
                                SongQueue.EnqueueFIFO(s);
                            }
                        }
                        else
                        {
                            SongQueue.Shuffle();
                        }
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
                        if (View.CurrentVisualizer is not null)
                        {
                            View.CurrentVisualizer.BoundPanel.Visibility = Visibility.Visible;
                            View.CurrentVisualizer.Start();
                        }

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
                        if (View.CurrentVisualizer is not null)
                        {
                            View.CurrentVisualizer.Stop();
                            View.CurrentVisualizer.BoundPanel.Visibility = Visibility.Collapsed;
                        }

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
                    _viewQueueCommand = new BaseCommand(() =>
                    {
                        if (View.navFrame.Content is QueuePage)
                        {
                            View.navFrame.Navigate(null);
                        }
                        else
                        {
                            QueuePage page = new(new(View));

                            View.navFrame.Navigate(page);
                        }
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

        #region Integrations

        public void SetupIntegrations()
        {
#if WINDOWS10_0_19041_0_OR_GREATER
            SetupMediaTransport();
#endif
            SetupDiscordRichPresence();
        }

        public void RemoveIntegrations()
        {
            RemoveRichPresence();
        }

        #region System Media Transport Controls
        #if WINDOWS10_0_19041_0_OR_GREATER
        private Windows.Media.SystemMediaTransportControls? _systemMediaTransportControls;

        private void SetupMediaTransport()
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

                UpdateMediaTransportTimeline();
            };

            _timer.Tick += (sender, e) =>
            {
                UpdateMediaTransport();
            };

            _fiveSecondTimer.Tick += (sender, e) =>
            {
                UpdateMediaTransportTimeline();
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

        private void UpdateMediaTransport()
        {
            if (_systemMediaTransportControls is not null)
            {
                _systemMediaTransportControls.IsPauseEnabled = IsPauseButton;
                _systemMediaTransportControls.IsPlayEnabled = !IsPauseButton;
                _systemMediaTransportControls.IsNextEnabled = !PeekNextSong(out _).IsEmpty;
                _systemMediaTransportControls.IsPreviousEnabled = SongHistory.Count > 0;
            }
        }

        private void UpdateMediaTransportTimeline()
        {
            if (_systemMediaTransportControls is null)
                return;

            var timeline = new Windows.Media.SystemMediaTransportControlsTimelineProperties();

            if (!CurrentSong.IsEmpty && CurrentSong.IsAvailable)
            {
                timeline.StartTime = TimeSpan.FromSeconds(0);
                timeline.EndTime = TimeSpan.FromSeconds(Duration);
                timeline.MinSeekTime = TimeSpan.FromSeconds(0);
                timeline.MaxSeekTime = TimeSpan.FromSeconds(Duration);
                timeline.Position = TimeSpan.FromSeconds(Position);
            }
            else
            {
                timeline.StartTime = TimeSpan.FromSeconds(0);
                timeline.EndTime = TimeSpan.FromSeconds(0);
                timeline.MinSeekTime = TimeSpan.FromSeconds(0);
                timeline.MaxSeekTime = TimeSpan.FromSeconds(0);
                timeline.Position = TimeSpan.FromSeconds(0);
            }

            _systemMediaTransportControls.UpdateTimelineProperties(timeline);
        }
#endif
        #endregion

        #region Discord Rich Presence
        private DiscordRPC.DiscordRpcClient? _discordClient;

        private void SetupDiscordRichPresence()
        {
            _discordClient = new DiscordRPC.DiscordRpcClient("919242231037169684", autoEvents: true);
            
            if (_discordClient.Initialize())
            {
                OnPlaybackStateChanged += DiscordRichPresence_OnPlaybackStateChanged;

                _fiveSecondTimer.Tick += DiscordRichPresence_FiveSecondTimerTick;
            }
        }

        private void DiscordRichPresence_FiveSecondTimerTick(object? sender, EventArgs e)
        {
            UpdateRichPresence();
        }

        private void DiscordRichPresence_OnPlaybackStateChanged(Song song, Un4seen.Bass.BASSActive newState, Un4seen.Bass.BASSActive oldState)
        {
            UpdateRichPresence();
        }

        private void UpdateRichPresence()
        {
            if (_discordClient is null)
                return;

            if (!CurrentSong.IsEmpty && CurrentSong.IsAvailable)
            {
                DiscordRPC.RichPresence presence = new()
                {
                    State = CurrentSong.Artist,
                    Details = CurrentSong.Title
                };

                presence.Assets = new DiscordRPC.Assets();

                switch (BassFacade.State)
                {
                    case Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING:
                        {
                            var utc = DateTime.UtcNow;

                            presence.Timestamps = new DiscordRPC.Timestamps()
                            {
                                Start = utc.Subtract(TimeSpan.FromSeconds(Position)),
                                End = utc.Add(TimeSpan.FromSeconds(Duration - Position))
                            };

                            presence.Assets.SmallImageText = "Playing";
                        }
                        break;
                    case Un4seen.Bass.BASSActive.BASS_ACTIVE_PAUSED:
                        {
                            presence.Assets.SmallImageText = "Paused";
                        }
                        break;
                    case Un4seen.Bass.BASSActive.BASS_ACTIVE_STALLED:
                        {
                            presence.Assets.SmallImageText = "Stalled";
                        }
                        break;
                    case Un4seen.Bass.BASSActive.BASS_ACTIVE_STOPPED:
                        {
                            presence.Assets.SmallImageText = "Stopped";
                        }
                        break;
                }

                _discordClient.SetPresence(presence);
            }
            else
            {
                _discordClient.ClearPresence();
            }
        }

        private void RemoveRichPresence()
        {
            if (_discordClient is not null)
            {
                OnPlaybackStateChanged -= DiscordRichPresence_OnPlaybackStateChanged;
                _fiveSecondTimer.Tick -= DiscordRichPresence_FiveSecondTimerTick;

                _discordClient.Deinitialize();

                _discordClient.Dispose();

                _discordClient = null;
            }
        }
        #endregion

        #endregion

        ~MusicPlayerViewModel()
        {
            RemoveIntegrations();
        }
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
