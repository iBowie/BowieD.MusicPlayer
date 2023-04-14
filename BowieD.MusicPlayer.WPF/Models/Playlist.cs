using BowieD.MusicPlayer.WPF.Data;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace BowieD.MusicPlayer.WPF.Models
{
    public struct Playlist : ISongSource
    {
        private IList<Song>? _loadedSongs;
        private byte[]? _pictureData;

        private Playlist(bool isEmpty)
        {
            ID = 0;
            Name = String.Empty;
            _loadedSongs = null;
            SongFileNames = Array.Empty<string>();
            _pictureData = null;
            IsEmpty = isEmpty;
        }
        public Playlist(long iD, string name, IList<Song> songs, byte[]? pictureData)
        {
            ID = iD;
            Name = name;
            _loadedSongs = songs;
            SongFileNames = songs.Select(d => d.FileName).ToList();
            _pictureData = pictureData;
            IsEmpty = false;
        }
        public Playlist(long iD, string name, IList<string> songFileNames, byte[]? pictureData)
        {
            ID = iD;
            Name = name;
            _loadedSongs = null;
            SongFileNames = songFileNames;
            _pictureData = pictureData;
            IsEmpty = false;
        }

        public bool IsEmpty { get; }
        public long ID { get; internal set; }
        public string Name { get; set; }
        public IList<string> SongFileNames { get; }
        public IList<Song> Songs
        {
            get
            {
                if (_loadedSongs is null)
                {
                    _loadedSongs = SongRepository.Instance.GetSongs(SongFileNames);
                }

                return _loadedSongs;
            }
        }
        public byte[] PictureData
        {
            get
            {
                if (_pictureData is null || _pictureData.Length == 0)
                {
                    _pictureData = Common.CoverAnalyzer.GenerateCoverArt(Songs, false);
                }

                return _pictureData;
            }
            set
            {
                _pictureData = value;
            }
        }

        public double TotalDuration
        {
            get
            {
                return Songs.Sum(d => d.Duration);
            }
        }

        public string DisplayToolTip
        {
            get
            {
                StringBuilder sb = new();

                sb.Append("track".ToQuantity(Songs.Count));

                sb.Append(", ");

                var span = TimeSpan.FromSeconds(TotalDuration);

                sb.Append(span.Humanize(precision: 2, minUnit: Humanizer.Localisation.TimeUnit.Second, collectionSeparator: " "));

                return sb.ToString();
            }
        }

        public string SourceName => Name;

        public static bool operator ==(Playlist a, Playlist b) => a.ID == b.ID;
        public static bool operator !=(Playlist a, Playlist b) => a.ID != b.ID;

        public IReadOnlyCollection<Song> GetSongs(Song currentSong)
        {
            int index = -1;

            for (int i = 0; i < Songs.Count; i++)
            {
                if (Songs[i].ID == currentSong.ID)
                {
                    index = i;
                    break;
                }
            }

            if (index >= 0)
            {
                return new ReadOnlyCollection<Song>(Songs.Skip(index + 1).Concat(Songs.Take(index)).ToList());
            }

            return new ReadOnlyCollection<Song>(Songs);
        }

        public static readonly Playlist EMPTY = new(true);
    }
}
