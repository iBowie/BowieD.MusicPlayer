using BowieD.MusicPlayer.WPF.Common;
using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.MVVM;
using BowieD.MusicPlayer.WPF.ViewModels;
using MahApps.Metro.Controls;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace BowieD.MusicPlayer.WPF.Views
{
    /// <summary>
    /// Логика взаимодействия для EditSongDetailsView.xaml
    /// </summary>
    public partial class EditSongDetailsView : MetroWindow
    {
        private readonly Song _original;

        public EditSongDetailsView(Song song)
        {
            InitializeComponent();

            _original = song;

            SongTitle = song.Title;
            SongArtist = song.Artist;
            SongAlbum = song.Album;
            SongCover = song.PictureData;
            SongYear = song.Year;

            mainGrid.DataContext = this;
        }

        private bool restoreState = false;
        private double savedPos;
        private bool autoPlay;
        private Song savedSong;

        public void LockState(MusicPlayerViewModel musicPlayer)
        {
            musicPlayer.LockAuto = true;

            restoreState = false;
            savedPos = 0.0;
            autoPlay = false;
            savedSong = Song.EMPTY;

            if (!musicPlayer.CurrentSong.IsEmpty && musicPlayer.CurrentSong.FileName == _original.FileName)
            {
                savedSong = musicPlayer.CurrentSong;
                savedPos = musicPlayer.Position01;
                autoPlay = BassFacade.State == Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING;
                restoreState = true;

                BassFacade.Stop();
            }
        }

        public void RestoreState(MusicPlayerViewModel musicPlayer)
        {
            if (restoreState)
            {
                musicPlayer.SetCurrentSong(savedSong, autoPlay);
                musicPlayer.Position01 = savedPos;
            }

            musicPlayer.LockAuto = false;
        }

        public Song ResultSong { get; private set; }

        private ICommand? _saveCommand;

        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand is null) 
                {
                    _saveCommand = new BaseCommand(() =>
                    {
                        string[] artists = SongArtist.Split(',')
                                                     .Where(d => !string.IsNullOrWhiteSpace(d))
                                                     .Select(d => d.Trim())
                                                     .ToArray();

                        using (TagLib.File tagFile = TagLib.File.Create(_original.FileName))
                        {
                            tagFile.Tag.Title = SongTitle;
                            tagFile.Tag.Performers = artists;
                            tagFile.Tag.Album = SongAlbum;
                            tagFile.Tag.Year = SongYear;

                            if (SongCover.Length > 0)
                            {
                                tagFile.Tag.Pictures = new TagLib.IPicture[1]
                                {
                                    new TagLib.Picture(new TagLib.ByteVector(SongCover))
                                };
                            }

                            tagFile.Save();
                        }

                        ResultSong = new Song(
                            _original.ID,
                            SongTitle,
                            string.Join(", ", artists),
                            SongAlbum,
                            SongYear,
                            _original.FileName,
                            SongCover,
                            _original.FullScreenPictureData);

                        DialogResult = true;
                        Close();
                    }, () =>
                    {
                        return true;
                    });
                }

                return _saveCommand;
            }
        }

        public string SongTitle
        {
            get => (string)GetValue(SongTitleProperty);
            set => SetValue(SongTitleProperty, value);
        }
        public string SongArtist
        {
            get => (string)GetValue(SongArtistProperty);
            set => SetValue(SongArtistProperty, value);
        }
        public string SongAlbum
        {
            get => (string)GetValue(SongAlbumProperty);
            set => SetValue(SongAlbumProperty, value);
        }
        public uint SongYear
        {
            get => (uint)GetValue(SongYearProperty);
            set => SetValue(SongYearProperty, value);
        }
        public byte[] SongCover
        {
            get => (byte[])GetValue(SongCoverProperty);
            set => SetValue(SongCoverProperty, value);
        }

        public static readonly DependencyProperty SongTitleProperty = DependencyProperty.Register("SongTitle", typeof(string), typeof(EditSongDetailsView), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty SongArtistProperty = DependencyProperty.Register("SongArtist", typeof(string), typeof(EditSongDetailsView), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty SongAlbumProperty = DependencyProperty.Register("SongAlbum", typeof(string), typeof(EditSongDetailsView), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty SongYearProperty = DependencyProperty.Register("SongYear", typeof(uint), typeof(EditSongDetailsView), new PropertyMetadata(uint.MinValue));
        public static readonly DependencyProperty SongCoverProperty = DependencyProperty.Register("SongCover", typeof(byte[]), typeof(EditSongDetailsView), new PropertyMetadata(Array.Empty<byte>()));
    }
}
