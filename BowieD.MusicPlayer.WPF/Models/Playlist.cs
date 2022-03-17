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
        public Playlist(long iD, string name, IList<Song> songs, byte[] pictureData)
        {
            ID = iD;
            Name = name;
            Songs = songs;
            PictureData = pictureData;
        }

        public long ID { get; }
        public string Name { get; set; }
        public IList<Song> Songs { get; }
        public byte[] PictureData { get; set; }

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


        public static implicit operator PlaylistInfo(Playlist playlist)
        {
            return new PlaylistInfo(playlist.ID, playlist.Name, playlist.Songs.Select(d => d.ID).ToList(), playlist.PictureData);
        }

        public static bool operator ==(Playlist a, Playlist b) => a.ID == b.ID;
        public static bool operator ==(Playlist a, PlaylistInfo b) => a.ID == b.ID;
        public static bool operator ==(PlaylistInfo a, Playlist b) => a.ID == b.ID;

        public static bool operator !=(Playlist a, Playlist b) => a.ID != b.ID;
        public static bool operator !=(Playlist a, PlaylistInfo b) => a.ID != b.ID;
        public static bool operator !=(PlaylistInfo a, Playlist b) => a.ID != b.ID;

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
    }
}
