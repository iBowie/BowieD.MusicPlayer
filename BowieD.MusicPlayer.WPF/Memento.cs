using BowieD.MusicPlayer.WPF.Data;
using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.ViewModels;
using BowieD.MusicPlayer.WPF.Views;
using System.IO;
using System.Linq;

namespace BowieD.MusicPlayer.WPF
{
    public static class Memento
    {
        private const string FILE_NAME = "state.dat";
        private const string MAGIC = "BowieD.MusicPlayer.WPF";
        private const int VERSION = 3;

        private const int SSOURCE_NONE_ID = -1;
        private const int SSOURCE_ALLSONGS_ID = 0;
        private const int SSOURCE_PLAYLIST_ID = 1;
        private const int SSOURCE_ALBUM_ID = 2;
        private const int SSOURCE_ARTIST_ID = 3;

        public static void SaveState(MainWindow mainWindow)
        {
            var vm = mainWindow.ViewModel;
            var mp = mainWindow.MusicPlayerViewModel;

            using FileStream fs = new(Path.Combine(DataFolder.DataDirectory, FILE_NAME), FileMode.Create, FileAccess.Write, FileShare.Read);
            using BinaryWriter bw = new(fs);

            bw.Write(VERSION);
            bw.Write(MAGIC);

            if (!mp.CurrentSong.IsEmpty)
            {
                bw.Write(true);

                bw.Write(mp.BassWrapper.State == Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING);
                bw.Write(mp.CurrentSong.ID);
                bw.Write(mp.Position01);
            }
            else
            {
                bw.Write(false);
            }

            bw.Write((double)mp.BassWrapper.UserVolume);
            bw.Write((byte)mp.LoopMode);
            bw.Write(mp.IsShuffleEnabled);

            bw.Write(mp.UserSongQueue.Count);
            foreach (var usq in mp.UserSongQueue)
                bw.Write(usq.ID);

            switch (mp.CurrentSongSource)
            {
                case Playlist pl:
                    {
                        bw.Write(SSOURCE_PLAYLIST_ID);

                        bw.Write(pl.ID);
                    }
                    break;
                case Views.Pages.AllSongsPage.AllSongsSource alls:
                    {
                        bw.Write(SSOURCE_ALLSONGS_ID);
                    }
                    break;
                case Album album:
                    {
                        bw.Write(SSOURCE_ALBUM_ID);

                        bw.Write(album.Name);
                    }
                    break;
                case Artist artist:
                    {
                        bw.Write(SSOURCE_ARTIST_ID);

                        bw.Write(artist.Name);
                    }
                    break;
                default:
                    {
                        bw.Write(SSOURCE_NONE_ID);

                        bw.Write(mp.SongQueue.Count);
                        foreach (var usq in mp.SongQueue)
                            bw.Write(usq.ID);

                        bw.Write(mp.SongHistory.Count);
                        foreach (var usq in mp.SongHistory)
                            bw.Write(usq.ID);
                    }
                    break;
            }
        }

        public static void RestoreState(MainWindow mainWindow)
        {
            string filePath = Path.Combine(DataFolder.DataDirectory, FILE_NAME);

            if (!File.Exists(filePath))
                return;

            var vm = mainWindow.ViewModel;
            var mp = mainWindow.MusicPlayerViewModel;

            try
            {
                using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using BinaryReader br = new(fs);

                int savedVer = br.ReadInt32();
                string readMagic = br.ReadString();

                if (savedVer <= VERSION && readMagic == MAGIC)
                {
                    if (br.ReadBoolean()) // current song not empty
                    {
                        bool autoPlay = br.ReadBoolean();
                        long songId = br.ReadInt64();
                        double pos01 = br.ReadDouble();

                        mp.BassWrapper.UserVolume = 0f;
                        mp.SetCurrentSong(SongRepository.Instance.GetSong(songId), autoPlay);

                        mp.Position01 = pos01;
                    }
                    else
                    {
                        mp.SetCurrentSong(Song.EMPTY, false);
                    }

                    mp.BassWrapper.UserVolume = (float)br.ReadDouble();
                    mp.LoopMode = (ELoopMode)br.ReadByte();

                    if (savedVer >= 2)
                    {
                        mp.IsShuffleEnabled = br.ReadBoolean();
                    }

                    mp.UserSongQueue.Clear();
                    mp.SongQueue.Clear();
                    mp.SongHistory.Clear();

                    int usqCount = br.ReadInt32();
                    for (int i = 0; i < usqCount; i++)
                    {
                        long songId = br.ReadInt64();
                        mp.UserSongQueue.Add(SongRepository.Instance.GetSong(songId));
                    }

                    int sourceId = br.ReadInt32();

                    switch (sourceId)
                    {
                        case SSOURCE_ALLSONGS_ID:
                            {
                                mp.CurrentSongSource = Views.Pages.AllSongsPage.AllSongsSource.Instance;
                            }
                            break;
                        case SSOURCE_PLAYLIST_ID:
                            {
                                long plId = br.ReadInt64();

                                mp.CurrentSongSource = (Playlist)PlaylistRepository.Instance.GetPlaylist(plId);
                            }
                            break;
                        case SSOURCE_ARTIST_ID:
                            {
                                string artistName = br.ReadString();

                                if (string.IsNullOrWhiteSpace(artistName))
                                    return;

                                var artists = SongRepository.Instance.GetAllArtists();

                                var artist = artists.SingleOrDefault(d => d.Name == artistName);

                                if (artist is null)
                                    return;

                                mp.CurrentSongSource = artist;
                            }
                            break;
                        case SSOURCE_ALBUM_ID:
                            {
                                string albumName = br.ReadString();

                                if (string.IsNullOrWhiteSpace(albumName))
                                    return;

                                var albums = SongRepository.Instance.GetAllAlbums();

                                var album = albums.SingleOrDefault(d => d.Name == albumName);

                                if (album is null)
                                    return;

                                mp.CurrentSongSource = album;
                            }
                            break;
                        case SSOURCE_NONE_ID:
                            {
                                int sqCount = br.ReadInt32();
                                for (int i = 0; i < sqCount; i++)
                                {
                                    long songId = br.ReadInt64();
                                    mp.SongQueue.Add(SongRepository.Instance.GetSong(songId));
                                }

                                int shCount = br.ReadInt32();
                                for (int i = 0; i < shCount; i++)
                                {
                                    long songId = br.ReadInt64();
                                    mp.SongHistory.Add(SongRepository.Instance.GetSong(songId));
                                }
                            }
                            break;
                        default:
                            throw new InvalidDataException();
                    }
                }
            }
            catch { }
        }
    }
}
