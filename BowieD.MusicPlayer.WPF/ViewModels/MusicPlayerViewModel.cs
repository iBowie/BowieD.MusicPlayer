using BowieD.MusicPlayer.WPF.Collections;
using BowieD.MusicPlayer.WPF.Common;
using BowieD.MusicPlayer.WPF.Extensions;
using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace BowieD.MusicPlayer.WPF.ViewModels
{
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
                TriggerPropertyChanged(nameof(Position), nameof(DisplayPosition), nameof(Duration), nameof(DisplayDuration), nameof(IsPauseButton));
                View.ViewModel.TriggerPropertyChanged(nameof(MainWindowViewModel.WindowTitle));

                if (CurrentSong.IsEmpty || Position >= Duration)
                {
                    NextTrackAuto();
                }
            };

            _timer.Start();
        }

        public event TrackChanged OnTrackChanged;

        private DispatcherTimer _timer;
        private Song _currentSong;
        private ELoopMode _loopMode;
        private readonly ObservableQueue<Song> _songQueue = new();
        private readonly ObservableStack<Song> _songHistory = new();
        private bool _isBigPicture = false;

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

                if (!value.IsEmpty)
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
            get => BassFacade.Volume;
            set => BassFacade.SetVolume(value);
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

        public string DisplayPosition => SecondsToText(Position);
        public string DisplayDuration => SecondsToText(Duration);

        public void PlayPlaylist(Playlist playlist, bool shuffle = false)
        {
            CurrentSong = Song.EMPTY;
            _songQueue.Clear();
            _songHistory.Clear();

            List<Song> safeSongs = new(playlist.Songs);

            if (shuffle)
                safeSongs.Shuffle();

            foreach (var s in safeSongs)
                _songQueue.Add(s);
        }

        public void NextTrackAuto()
        {
            if (LoopMode == ELoopMode.CURRENT && !CurrentSong.IsEmpty)
            {
                Position = 0;
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

        }

        public void PrevTrack()
        {

        }

        private string SecondsToText(double seconds)
        {
            TimeSpan span = TimeSpan.FromSeconds(seconds);

            if (span.TotalHours >= 1)
            {
                return span.ToString("hh':'mm':'ss");
            }

            return span.ToString("mm':'ss");
        }

        #region Commands

        private ICommand 
            _nextTrackCommand,
            _loopCommand,
            _shuffleCommand,
            _prevTrackCommand,
            _showBigPictureCommand,
            _collapseBigPictureCommand,
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
                    });
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
                    });
                }

                return _collapseBigPictureCommand;
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
