﻿using BowieD.MusicPlayer.WPF.Common;
using BowieD.MusicPlayer.WPF.Data;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace BowieD.MusicPlayer.WPF.Models
{
    public struct Song
    {
        private Song(bool isEmpty) : this()
        {
            IsEmpty = isEmpty;
        }

        public Song(long iD, string title, string artist, string album, uint year, string fileName, byte[] pictureData, byte[] fullScreenPictureData)
        {
            ID = iD;
            Title = title;
            Artist = artist;
            Album = album;
            Year = year;
            FileName = fileName;
            PictureData = pictureData;
            IsEmpty = false;
            FullScreenPictureData = fullScreenPictureData;
        }

        public long ID { get; internal set; }
        public string Title { get; private set; }
        public string Artist { get; private set; }
        public string Album { get; private set; }
        public uint Year { get; private set; }
        public string FileName { get; }
        public byte[] PictureData { get; private set; }
        public byte[] FullScreenPictureData { get; internal set; }
        public bool IsEmpty { get; }
        public bool IsAvailable
        {
            get
            {
                return FileTool.CheckFileValid(FileName, BassFacade.SupportedExtensions);
            }
        }
        public double Duration
        {
            get
            {
                return FileTool.GetDuration(FileName);
            }
        }

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

        public static bool operator ==(Song a, Song b) => a.ID == b.ID;

        public static bool operator !=(Song a, Song b) => a.ID != b.ID;

        public void UpdateFromDatabase()
        {
            Song newVersion = SongRepository.Instance.GetSong(ID);

            if (newVersion.IsEmpty)
                return;

            this.Title = newVersion.Title;
            this.Artist = newVersion.Artist;
            this.Album = newVersion.Album;
            this.PictureData = newVersion.PictureData;
            this.FullScreenPictureData = newVersion.FullScreenPictureData;
            this.Year = newVersion.Year;
        }
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

        public static bool operator ==(PlaylistInfo a, PlaylistInfo b) => a.ID == b.ID;

        public static bool operator !=(PlaylistInfo a, PlaylistInfo b) => a.ID != b.ID;
    }

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
