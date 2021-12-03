using BowieD.MusicPlayer.WPF.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BowieD.MusicPlayer.WPF.Models
{
    public struct Song
    {
        private Song(bool isEmpty) : this()
        {
            IsEmpty = isEmpty;
        }

        public Song(long iD, string title, string artist, string album, uint year, string fileName, byte[] pictureData)
        {
            ID = iD;
            Title = title;
            Artist = artist;
            Album = album;
            Year = year;
            FileName = fileName;
            PictureData = pictureData;
            IsEmpty = false;
        }

        public long ID { get; internal set; }
        public string Title { get; }
        public string Artist { get; }
        public string Album { get; }
        public uint Year { get; }
        public string FileName { get; }
        public byte[] PictureData { get; }
        public bool IsEmpty { get; }

        public string DisplayYear
        {
            get
            {
                if (Year == 0)
                    return string.Empty;

                return Year.ToString();
            }
        }

        public static readonly Song EMPTY = new(true);
    }

    public struct PlaylistInfo
    {
        public PlaylistInfo(long iD, string name, IList<long> songIDs, byte[] pictureData)
        {
            ID = iD;
            Name = name;
            SongIDs = songIDs;
            PictureData = pictureData;

            IsEmpty = false;
        }

        private PlaylistInfo(bool empty)
        {
            ID = 0;
            Name = string.Empty;
            SongIDs = Array.Empty<long>();
            PictureData = Array.Empty<byte>();

            this.IsEmpty = empty;
        }

        public long ID { get; internal set; }
        public string Name { get; }
        public IList<long> SongIDs { get; }
        public byte[] PictureData { get; }
        public bool IsEmpty { get; }

        public static explicit operator Playlist(PlaylistInfo info)
        {
            return new Playlist(info.ID, info.Name, SongRepository.Instance.GetSongs(info.SongIDs), info.PictureData);
        }

        public static readonly PlaylistInfo EMPTY = new(true);
    }

    public struct Playlist
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
        public string DisplayToolTip
        {
            get => $"{Songs.Count} tracks";
        }

        public static implicit operator PlaylistInfo(Playlist playlist)
        {
            return new PlaylistInfo(playlist.ID, playlist.Name, playlist.Songs.Select(d => d.ID).ToList(), playlist.PictureData);
        }
    }
}
