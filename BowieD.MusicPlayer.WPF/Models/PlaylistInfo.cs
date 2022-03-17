using BowieD.MusicPlayer.WPF.Data;
using System;
using System.Collections.Generic;

namespace BowieD.MusicPlayer.WPF.Models
{
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
}
