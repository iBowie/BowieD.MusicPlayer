using BowieD.MusicPlayer.WPF.Common;
using BowieD.MusicPlayer.WPF.Data;
using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.ViewModels;
using BowieD.MusicPlayer.WPF.Views;
using System.IO;

namespace BowieD.MusicPlayer.WPF
{
    public static class Memento
    {
        private const string FILE_NAME = "state.dat";
        private const string MAGIC = "BowieD.MusicPlayer.WPF";
        private const int VERSION = 2;

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

                bw.Write(BassFacade.State == Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING);
                bw.Write(mp.CurrentSong.ID);
                bw.Write(mp.Position01);
            }
            else
            {
                bw.Write(false);
            }

            bw.Write(mp.Volume);
            bw.Write((byte)mp.LoopMode);
            bw.Write(mp.IsShuffleEnabled);

            bw.Write(mp.UserSongQueue.Count);
            foreach (var usq in mp.UserSongQueue)
                bw.Write(usq.ID);

            bw.Write(mp.SongQueue.Count);
            foreach (var usq in mp.SongQueue.GetUnshuffled())
                bw.Write(usq.ID);

            bw.Write(mp.SongHistory.Count);
            foreach (var usq in mp.SongHistory)
                bw.Write(usq.ID);
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

                        mp.SetCurrentSong(SongRepository.Instance.GetSong(songId), autoPlay);

                        mp.Position01 = br.ReadDouble();
                    }
                    else
                    {
                        mp.SetCurrentSong(Song.EMPTY, false);
                    }

                    mp.Volume = br.ReadDouble();
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

                    int sqCount = br.ReadInt32();
                    for (int i = 0; i < sqCount; i++)
                    {
                        long songId = br.ReadInt64();
                        mp.SongQueue.Add(SongRepository.Instance.GetSong(songId));
                    }

                    if (mp.IsShuffleEnabled)
                        mp.SongQueue.IsShuffled = true;

                    int shCount = br.ReadInt32();
                    for (int i = 0; i < shCount; i++)
                    {
                        long songId = br.ReadInt64();
                        mp.SongHistory.Add(SongRepository.Instance.GetSong(songId));
                    }
                }
            }
            catch { }
        }
    }
}
