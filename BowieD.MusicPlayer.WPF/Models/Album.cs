using BowieD.MusicPlayer.WPF.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BowieD.MusicPlayer.WPF.Models
{
    public sealed class Album : ISongSource
    {
        private readonly List<Song> _loadedSongs;
        private bool _hasLoaded;

        public Album(string name, IList<string> songFileNames)
        {
            _hasLoaded = false;
            _loadedSongs = new List<Song>();

            this.Name = name;
            this.SongFileNames = songFileNames;
        }

        public string Name { get; }
        public IList<string> SongFileNames { get; }

        public IList<Song> Songs
        {
            get
            {
                if (!_hasLoaded)
                {
                    _loadedSongs.AddRange(SongRepository.Instance.GetSongs(SongFileNames));

                    _hasLoaded = true;
                }

                return _loadedSongs;
            }
        }

        public string SourceName => Name;
        public IReadOnlyCollection<Song> GetSongs(Song currentSong)
        {
            int index = -1;

            IList<Song> songs = Songs;

            for (int i = 0; i < songs.Count; i++)
            {
                if (songs[i].ID == currentSong.ID)
                {
                    index = i;
                    break;
                }
            }

            if (index >= 0)
            {
                return new ReadOnlyCollection<Song>(songs.Skip(index + 1).Concat(songs.Take(index)).ToList());
            }

            return new ReadOnlyCollection<Song>(songs);
        }
    }
}
