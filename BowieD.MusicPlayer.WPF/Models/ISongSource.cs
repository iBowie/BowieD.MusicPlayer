using System.Collections.Generic;

namespace BowieD.MusicPlayer.WPF.Models
{
    public interface ISongSource
    {
        string SourceName { get; }
        IReadOnlyCollection<Song> GetSongs(Song currentSong);
    }
}
