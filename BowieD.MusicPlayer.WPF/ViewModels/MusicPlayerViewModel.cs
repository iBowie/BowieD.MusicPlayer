using BowieD.MusicPlayer.WPF.Collections;
using BowieD.MusicPlayer.WPF.Common;
using BowieD.MusicPlayer.WPF.Configuration;
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
using System.Windows.Input;
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

            BassWrapper = new BassWrapper(_timer);

            BassWrapper.SongEnded += (sender, e) =>
            {
                NextTrackAuto();
            };

            _timer.Tick += (sender, e) =>
            {
                Un4seen.Bass.BASSActive newState = BassWrapper.State;

                if (newState != _prevState)
                {
                    OnPlaybackStateChanged?.Invoke(CurrentSong, newState, _prevState);
                }

                _prevState = newState;

                TriggerPropertyChanged(nameof(Position01), nameof(IsUpcomingSongVisible), nameof(IsPauseButton), nameof(UpcomingSongSlider));
                View.ViewModel.TriggerPropertyChanged(nameof(MainWindowViewModel.WindowTitle));
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

            AppSettings.Instance.StartTrackingSetting((settings) =>
            {
                if (settings.EnableDiscordRichPresence)
                    SetupDiscordRichPresence();
                else
                    RemoveRichPresence();
            }, nameof(AppSettings.EnableDiscordRichPresence));
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
        private ISongSource? _currentSongSource;

        public BassWrapper BassWrapper { get; }

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
                    BassWrapper.LoadFile(value.FileName);

                    BassWrapper.Play();

                    OnTrackChanged?.Invoke(value);
                }
                else
                {
                    BassWrapper.Stop();
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

        public bool IsBigPicture
        {
            get => _isBigPicture;
            set => ChangeProperty(ref _isBigPicture, value, nameof(IsBigPicture));
        }
        public bool IsPauseButton
        {
            get
            {
                if (BassWrapper.State == Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING)
                    return true;

                return false;
            }
        }
        public double Position01
        {
            get => BassWrapper.Position / BassWrapper.Duration;
            set => BassWrapper.Position = value * BassWrapper.Duration;
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
                return BassWrapper.State switch
                {
                    Un4seen.Bass.BASSActive.BASS_ACTIVE_STOPPED => TaskbarItemProgressState.None,
                    Un4seen.Bass.BASSActive.BASS_ACTIVE_STALLED => TaskbarItemProgressState.Error,
                    Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING => TaskbarItemProgressState.Normal,
                    Un4seen.Bass.BASSActive.BASS_ACTIVE_PAUSED => TaskbarItemProgressState.Paused,
                    _ => throw new Exception(),
                };
            }
        }
        public bool IsUpcomingSongVisible
        {
            get => LoopMode != ELoopMode.CURRENT && SongQueue.Count > 0 && BassWrapper.Duration - BassWrapper.Position < UPCOMING_SONG_PRETIME;
        }
        public double UpcomingSongSlider
        {
            get
            {
                double curDur = BassWrapper.Duration;

                if (curDur < UPCOMING_SONG_PRETIME)
                    return Position01;

                double curPos = BassWrapper.Position;
                double preTime = UPCOMING_SONG_PRETIME;

                return Math.Clamp(1.0 - ((curDur - curPos) / preTime), 0, 1);
            }
        }
        public bool IsUserQueueVisible => UserSongQueue.Count > 0;

        private void Clean()
        {
            CurrentSongSource = null;
            CurrentSong = Song.EMPTY;
            SongQueue.Clear();
            SongHistory.Clear();
        }

        public void PlaySource(ISongSource songSource, bool shuffle = false)
        {
            Clean();

            if (shuffle)
                IsShuffleEnabled = true;

            CurrentSongSource = songSource;
        }

        public void PlaySongFromSource(Song song, ISongSource songSource, bool shuffle = false)
        {
            Clean();

            if (shuffle)
                IsShuffleEnabled = true;

            CurrentSong = song;
            CurrentSongSource = songSource;
        }

        [Obsolete]
        public void PlayPlaylist(Playlist playlist, bool shuffle = false) => PlaySource(playlist, shuffle);
        [Obsolete]
        public void PlaySongFromPlaylist(Song song, Playlist playlist, bool shuffle = false) => PlaySongFromSource(song, playlist, shuffle);

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
                BassWrapper.Pause(true);
        }

        #region Commands

        private ICommand?
            _nextTrackCommand,
            _loopCommand,
            _shuffleCommand,
            _prevTrackCommand,
            _showBigPictureCommand,
            _collapseBigPictureCommand,
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
                        switch (BassWrapper.State)
                        {
                            case Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING:
                                BassWrapper.Pause();
                                break;
                            case Un4seen.Bass.BASSActive.BASS_ACTIVE_PAUSED:
                                BassWrapper.Resume();
                                break;
                            case Un4seen.Bass.BASSActive.BASS_ACTIVE_STOPPED:
                                BassWrapper.LoadFile(CurrentSong.FileName);
                                BassWrapper.Play();
                                break;
                        }
                    }, () =>
                    {
                        return BassWrapper.State == Un4seen.Bass.BASSActive.BASS_ACTIVE_PAUSED ||
                               BassWrapper.State == Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING ||
                               (BassWrapper.State == Un4seen.Bass.BASSActive.BASS_ACTIVE_STOPPED && !CurrentSong.IsEmpty && CurrentSong.IsAvailable);
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
                        if (BassWrapper.Position >= 15)
                        {
                            BassWrapper.Position = 0;
                        }
                        else
                        {
                            PrevTrack();
                        }
                    }, () =>
                    {
                        return (BassWrapper.State == Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING && BassWrapper.Position >= 15) || !PeekPrevSong(out _).IsEmpty;
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
                    }, () => IsBigPicture);
                }

                return _collapseBigPictureCommand;
            }
        }
        private object? _prevContentBeforeQueue;
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
                            View.navFrame.Navigate(_prevContentBeforeQueue);

                            _prevContentBeforeQueue = null;
                        }
                        else
                        {
                            _prevContentBeforeQueue = View.navFrame.Content;

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
            if (AppSettings.Instance.EnableDiscordRichPresence)
            {
                SetupDiscordRichPresence();
            }
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
                _systemMediaTransportControls.PlaybackStatus = BassWrapper.State switch
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
                timeline.EndTime = TimeSpan.FromSeconds(BassWrapper.Duration);
                timeline.MinSeekTime = TimeSpan.FromSeconds(0);
                timeline.MaxSeekTime = TimeSpan.FromSeconds(BassWrapper.Duration);
                timeline.Position = TimeSpan.FromSeconds(BassWrapper.Position);
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
            RemoveRichPresence();

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

                switch (BassWrapper.State)
                {
                    case Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING:
                        {
                            var utc = DateTime.UtcNow;

                            presence.Timestamps = new DiscordRPC.Timestamps()
                            {
                                Start = utc.Subtract(TimeSpan.FromSeconds(BassWrapper.Position)),
                                End = utc.Add(TimeSpan.FromSeconds(BassWrapper.Duration - BassWrapper.Position))
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
