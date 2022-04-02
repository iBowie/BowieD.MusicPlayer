using BowieD.MusicPlayer.WPF.Common;
using BowieD.MusicPlayer.WPF.Data;

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
        public string Title { get; private set; }
        public string Artist { get; private set; }
        public string Album { get; private set; }
        public uint Year { get; private set; }
        public string FileName { get; private set; }
        public byte[] PictureData { get; private set; }
        public bool IsEmpty { get; }
        public bool IsAvailable
        {
            get
            {
                return FileTool.CheckFileValid(FileName, BassWrapper.SupportedExtensions);
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
            this.Year = newVersion.Year;
        }

        public void UpdateFileName(string newFileName)
        {
            this.FileName = newFileName;
        }
    }
}
