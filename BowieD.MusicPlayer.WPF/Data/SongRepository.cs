using BowieD.MusicPlayer.WPF.Common;
using BowieD.MusicPlayer.WPF.Extensions;
using BowieD.MusicPlayer.WPF.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BowieD.MusicPlayer.WPF.Data
{
    public sealed class SongRepository : BaseRepository
    {
        #region BOILER PLATE
        private static SongRepository? _instance;
        public static SongRepository Instance
        {
            get
            {
                if (_instance is null)
                {
                    _instance = new SongRepository();
                }

                return _instance;
            }
        }

        private const long CURRENT_VERSION = 1;
        private const string
            TABLE_NAME = "songs_cache",
            COL_ID = "id",
            COL_TITLE = "title",
            COL_ALBUM = "album",
            COL_ARTIST = "artist",
            COL_YEAR = "year",
            COL_COVER = "cover",
            COL_FILE_NAME = "fileName";

        public SongRepository() : base(Path.Combine(DataFolder.DataDirectory, "library.db")) { }

        protected override void OnPrepare()
        {
            ExecuteNonQuery($"CREATE TABLE IF NOT EXISTS {TABLE_NAME} (" +
                $"{COL_ID} INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, " +
                $"{COL_TITLE} VARCHAR(128), " +
                $"{COL_ALBUM} VARCHAR(128), " +
                $"{COL_ARTIST} VARCHAR(128), " +
                $"{COL_YEAR} INTEGER, " +
                $"{COL_COVER} BLOB, " +
                $"{COL_FILE_NAME} VARCHAR(1024) NOT NULL UNIQUE" +
                $")");

            var version = (long)ExecuteScalar("PRAGMA user_version");

            // migrations here

            ExecuteNonQuery($"PRAGMA user_version = {CURRENT_VERSION}");
        }
        #endregion

        public Song GetSong(long id)
        {
            string sql = $"SELECT * FROM {TABLE_NAME} WHERE {COL_ID} = @id LIMIT 1";

            using var con = CreateConnection();

            con.Open();

            using var com = new SQLiteCommand(sql, con);

            com.Parameters.Add("@id", System.Data.DbType.Int64).Value = id;

            using var reader = com.ExecuteReader(System.Data.CommandBehavior.KeyInfo);

            if (reader.HasRows && reader.Read())
            {
                return ReadSong(reader, id);
            }

            return Song.EMPTY;
        }

        public Song GetOrAddSong(string fileName)
        {
            string sql = $"SELECT * FROM {TABLE_NAME} WHERE {COL_FILE_NAME} = @fileName LIMIT 1";

            using var con = CreateConnection();
            con.Open();
            using var com = new SQLiteCommand(sql, con);
            com.Parameters.Add("@fileName", DbType.String).Value = fileName;
            using var reader = com.ExecuteReader(CommandBehavior.KeyInfo);

            if (reader.HasRows)
            {
                if (reader.Read())
                {
                    var song = ReadSong(reader);

                    return song;
                }

                throw new Exception();
            }
            else
            {
                return AddNewSong(fileName);
            }
        }

        public IList<Song> GetSongs(IEnumerable<string> songFileNames)
        {
            List<Song> result = new();

            foreach (var songFn in songFileNames)
            {
                result.Add(GetOrAddSong(songFn));
            }

            return result;
        }

        public IList<Song> GetAllSongs()
        {
            List<Song> result = new();

            string sql = $"SELECT * FROM {TABLE_NAME}";

            using var con = CreateConnection();

            con.Open();

            using var com = new SQLiteCommand(sql, con);

            using var reader = com.ExecuteReader(System.Data.CommandBehavior.KeyInfo);

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    result.Add(ReadSong(reader));
                }
            }

            con.Close();

            return result;
        }

        public async Task<IList<string>> GetAllSongFileNamesAsync(CancellationToken cancellationToken)
        {
            string sql = $"SELECT {COL_FILE_NAME} FROM {TABLE_NAME}";

            using var con = CreateConnection();

            con.Open();

            using var com = con.CreateCommand();
            com.CommandText = sql;

            using var reader = await com.ExecuteReaderAsync(cancellationToken);

            List<string> lst = new();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    lst.Add(reader.GetString(COL_FILE_NAME));
                }
            }

            return lst;
        }

        public void UpdateSong(Song song, bool updateFromMeta = true)
        {
            Song meta;

            if (updateFromMeta)
                meta = GetSongMetadata(song.FileName);
            else
                meta = song;

            const string
                PARAM_ID = "@id",
                PARAM_TITLE = "@title",
                PARAM_ARTIST = "@artist",
                PARAM_ALBUM = "@album",
                PARAM_YEAR = "@year",
                PARAM_COVER = "@cover",
                PARAM_FILE_NAME = "@fileName";

            string sql = $"UPDATE {TABLE_NAME} " +
                $"SET {COL_TITLE} = {PARAM_TITLE}, {COL_ARTIST} = {PARAM_ARTIST}, " +
                $"{COL_ALBUM} = {PARAM_ALBUM}, {COL_YEAR} = {PARAM_YEAR}, " +
                $"{COL_COVER} = {PARAM_COVER}, {COL_FILE_NAME} = {PARAM_FILE_NAME} " +
                $"WHERE {COL_ID} = {PARAM_ID}";

            using var con = CreateConnection();

            con.Open();

            using var com = new SQLiteCommand(sql, con);

            com.Parameters.Add(PARAM_ID, DbType.Int32).Value = song.ID;
            com.Parameters.Add(PARAM_TITLE, DbType.String).Value = meta.Title;
            com.Parameters.Add(PARAM_ARTIST, DbType.String).Value = meta.Artist;
            com.Parameters.Add(PARAM_ALBUM, DbType.String).Value = meta.Album;
            com.Parameters.Add(PARAM_YEAR, DbType.UInt32).Value = meta.Year > 0 ? meta.Year : DBNull.Value;
            com.Parameters.Add(PARAM_COVER, DbType.Binary).Value = meta.PictureData;
            com.Parameters.Add(PARAM_FILE_NAME, DbType.String).Value = meta.FileName;
            
            com.ExecuteNonQuery();
        }

        public void RemoveSong(Song song)
        {
            string sql =
                $"DELETE FROM {TABLE_NAME} " +
                $"WHERE {COL_ID} = @id";

            using var con = CreateConnection();

            con.Open();

            using var com = new SQLiteCommand(sql, con);

            com.Parameters.Add("@id", DbType.Int32).Value = song.ID;

            com.ExecuteNonQuery();
        }

        public static readonly string[] ARTIST_SEPARATORS = new string[2] { ";", "/" };

        public IList<Artist> GetAllArtists()
        {
            Dictionary<string, List<string>> data = new();

            {
                string sql = $"SELECT {COL_FILE_NAME},{COL_ARTIST} FROM {TABLE_NAME}";

                using var con = CreateConnection();

                con.Open();

                using var com = new SQLiteCommand(sql, con);

                using var reader = com.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var artistNamesRaw = reader.GetString(COL_ARTIST);

                        if (string.IsNullOrWhiteSpace(artistNamesRaw))
                            continue;

                        string[] artistNames = artistNamesRaw.Split(ARTIST_SEPARATORS, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                        if (artistNames.Length == 0)
                            continue;

                        var songFn = reader.GetString(COL_FILE_NAME);

                        foreach (var artist in artistNames)
                        {
                            if (!data.ContainsKey(artist))
                                data.Add(artist, new List<string>());

                            data[artist].Add(songFn);
                        }
                    }
                }

                con.Close();
            }

            List<Artist> res = new();

            foreach (var kv in data)
            {
                Artist a = new(kv.Key, kv.Value);

                res.Add(a);
            }

            return res;
        }

        public IList<Album> GetAllAlbums()
        {
            Dictionary<string, List<string>> data = new();

            {
                string sql = $"SELECT {COL_FILE_NAME},{COL_ALBUM} FROM {TABLE_NAME}";

                using var con = CreateConnection();

                con.Open();

                using var com = new SQLiteCommand(sql, con);

                using var reader = com.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var albumName = reader.GetString(COL_ALBUM);

                        if (string.IsNullOrWhiteSpace(albumName))
                            continue;

                        var songFn = reader.GetString(COL_FILE_NAME);

                        if (!data.ContainsKey(albumName))
                            data.Add(albumName, new List<string>());

                        data[albumName].Add(songFn);
                    }
                }

                con.Close();
            }

            List<Album> res = new();

            foreach (var kv in data)
            {
                Album a = new(kv.Key, kv.Value);

                res.Add(a);
            }

            return res;
        }

        public async Task SearchForMusicAsync(string src, IProgress<double> progress, CancellationToken cancellationToken)
        {
            progress.Report(double.NaN);

            var files = Directory.GetFiles(src, "*.*", SearchOption.AllDirectories);
            var libraryFiles = await GetAllSongFileNamesAsync(cancellationToken);

            progress.Report(0.0);

            int i = 0;
            int total = files.Length;

            foreach (var file in files)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (!libraryFiles.Remove(file))
                {
                    if (FileTool.CheckFileValid(file, BassWrapper.SupportedExtensions))
                    {
                        try
                        {
                            var song = GetOrAddSong(file);
                        }
                        catch { }
                    }
                }

                progress.Report(i++ / (double)total);

                if (i % 50 == 0)
                    await Task.Delay(10, cancellationToken);
                else
                    await Task.Yield();
            }
        }

        private Song AddNewSong(string fileName)
        {
            Song meta = GetSongMetadata(fileName);

            const string
                PARAM_TITLE = "@title",
                PARAM_ARTIST = "@artist",
                PARAM_ALBUM = "@album",
                PARAM_YEAR = "@year",
                PARAM_COVER = "@cover",
                PARAM_FILE_NAME = "@fileName";

            string sql = $"INSERT INTO {TABLE_NAME} " +
                $"({COL_TITLE}, {COL_ARTIST}, {COL_ALBUM}, {COL_YEAR}, {COL_COVER}, {COL_FILE_NAME}) " +
                $"values({PARAM_TITLE}, {PARAM_ARTIST}, {PARAM_ALBUM}, {PARAM_YEAR}, {PARAM_COVER}, {PARAM_FILE_NAME})";

            using var con = CreateConnection();

            con.Open();

            using var com = new SQLiteCommand(sql, con);

            com.Parameters.Add(PARAM_TITLE, DbType.String).Value = meta.Title;
            com.Parameters.Add(PARAM_ARTIST, DbType.String).Value = meta.Artist;
            com.Parameters.Add(PARAM_ALBUM, DbType.String).Value = meta.Album;
            com.Parameters.Add(PARAM_YEAR, DbType.UInt32).Value = meta.Year > 0 ? meta.Year : DBNull.Value;
            com.Parameters.Add(PARAM_COVER, DbType.Binary).Value = meta.PictureData;
            com.Parameters.Add(PARAM_FILE_NAME, DbType.String).Value = meta.FileName;

            com.ExecuteNonQuery();

            meta.ID = con.LastInsertRowId;

            return meta;
        }

        private static Song GetSongMetadata(string fileName)
        {
            using var meta = TagLib.File.Create(fileName);

            string title = meta?.Tag?.Title ?? Path.GetFileNameWithoutExtension(fileName) ?? string.Empty;
            string artist = string.Join(", ", meta?.Tag?.Performers ?? Array.Empty<string>()) ?? string.Empty;
            string album = meta?.Tag?.Album ?? string.Empty;
            uint year = meta?.Tag?.Year ?? 0;

            byte[]? picture = null;

            if (meta?.Tag?.Pictures?.Length >= 1)
            {
                picture = meta.Tag.Pictures[0].Data.Data;
            }

            if (picture is null)
            {
                string songDir = Path.GetDirectoryName(fileName);

                string[] checkFileNames = new string[]
                {
                    "cover",
                    "album",
                    "picture",
                    "art",
                    "folder",
                    Path.GetFileNameWithoutExtension(fileName)
                };

                string[] checkExts = new string[]
                {
                    "png",
                    "jpg",
                    "jpeg",
                    "bmp"
                };

                foreach (var cfn in checkFileNames)
                {
                    if (picture is not null)
                        break;

                    foreach (var ext in checkExts)
                    {
                        string combined = Path.Combine(songDir, $"{cfn}.{ext}");

                        if (File.Exists(combined))
                        {
                            try
                            {
                                picture = File.ReadAllBytes(combined);
                                break;
                            }
                            catch { picture = null; }
                        }
                    }
                }
            }

            if (picture is not null && picture.Length > 0)
            {
                picture = ImageTool.ResizeInByteArray(picture, 700, 700);
            }

            return new Song(0, title, artist, album, year, fileName, picture ?? Array.Empty<byte>());
        }

        private static Song ReadSong(SQLiteDataReader reader, long? id = null)
        {
            long songId = id ?? reader.GetInt64(COL_ID);
            string title = reader.GetString(COL_TITLE);
            string artist = reader.GetString(COL_ARTIST);
            string album = reader.GetString(COL_ALBUM);
            uint year = reader.IsDBNull(COL_YEAR) ? 0 : (uint)reader.GetInt32(COL_YEAR);
            string fileName = reader.GetString(COL_FILE_NAME);
            byte[] picture;

            using (var blob = reader.GetBlob(COL_COVER, true))
            {
                int picLength = blob.GetCount();

                picture = new byte[picLength];
            }

            reader.GetBytes(COL_COVER, 0, picture, 0, picture.Length);

            return new Song(songId, title, artist, album, year, fileName, picture);
        }
    }
}
