using BowieD.MusicPlayer.WPF.Extensions;
using BowieD.MusicPlayer.WPF.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace BowieD.MusicPlayer.WPF.Data
{
    public sealed class PlaylistRepository : BaseRepository
    {
        private static PlaylistRepository? _instance;
        public static PlaylistRepository Instance
        {
            get
            {
                if (_instance is null)
                {
                    _instance = new PlaylistRepository();
                }

                return _instance;
            }
        }

        private const string
            TABLE_NAME = "playlists",
            COL_ID = "id",
            COL_NAME = "name",
            COL_COVER = "cover",
            COL_SONGS = "songs";

        public PlaylistRepository() : base(Path.Combine(DataFolder.DataDirectory, "user_playlists.db")) { }

        protected override void OnPrepare()
        {
            ExecuteNonQuery($"CREATE TABLE IF NOT EXISTS {TABLE_NAME} (" +
                $"{COL_ID} INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, " +
                $"{COL_NAME} VARCHAR(128), " +
                $"{COL_COVER} BLOB, " +
                $"{COL_SONGS} JSON DEFAULT('[]')" +
                $")");
        }

        public Playlist GetPlaylist(long id)
        {
            string sql = $"SELECT * FROM {TABLE_NAME} WHERE {COL_ID} = @id";

            using var con = CreateConnection();

            con.Open();

            using var com = new SQLiteCommand(sql, con);

            com.Parameters.Add("@id", DbType.Int64).Value = id;

            using var reader = com.ExecuteReader(CommandBehavior.KeyInfo);

            Playlist result;

            if (reader.HasRows && reader.Read())
            {
                result = ReadPlaylist(reader, id);
            }
            else
            {
                result = Playlist.EMPTY;
            }

            con.Close();

            return result;
        }

        public IList<Playlist> GetAllPlaylists()
        {
            List<Playlist> result = new();

            string sql = $"SELECT * FROM {TABLE_NAME}";

            using var con = CreateConnection();

            con.Open();

            using var com = new SQLiteCommand(sql, con);

            using var reader = com.ExecuteReader(CommandBehavior.KeyInfo);

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    var pl = ReadPlaylist(reader);

                    result.Add(pl);
                }
            }


            con.Close();

            return result;
        }

        public void AddNewPlaylist(ref Playlist playlist)
        {
            string sql =
                $"INSERT INTO {TABLE_NAME} " +
                $"({COL_NAME}, {COL_COVER}, {COL_SONGS}) " +
                $"values (@name, @cover, @songs)";

            long liri;

            using (var con = CreateConnection())
            {
                con.Open();

                using var com = new SQLiteCommand(sql, con);

                com.Parameters.Add("@name", DbType.String).Value = playlist.Name;
                com.Parameters.Add("@cover", DbType.Binary).Value = playlist.PictureData;
                com.Parameters.Add("@songs", DbType.String).Value = System.Text.Json.JsonSerializer.Serialize(playlist.SongFileNames);

                com.ExecuteNonQuery();

                liri = con.LastInsertRowId;
            }

            playlist.ID = liri;
        }

        public void UpdatePlaylist(Playlist playlist)
        {
            string sql =
                $"UPDATE {TABLE_NAME} " +
                $"SET {COL_NAME} = @name, {COL_COVER} = @cover, {COL_SONGS} = @songs " +
                $"WHERE {COL_ID} = @id";

            using var con = CreateConnection();

            con.Open();

            using var com = new SQLiteCommand(sql, con);

            com.Parameters.Add("@id", DbType.Int64).Value = playlist.ID;
            com.Parameters.Add("@name", DbType.String).Value = playlist.Name;
            com.Parameters.Add("@cover", DbType.Binary).Value = playlist.PictureData;
            com.Parameters.Add("@songs", DbType.String).Value = System.Text.Json.JsonSerializer.Serialize(playlist.SongFileNames);

            com.ExecuteNonQuery();
        }

        public void RemovePlaylist(Playlist playlist)
        {
            string sql =
                $"DELETE FROM {TABLE_NAME} " +
                $"WHERE {COL_ID} = @id";

            using var con = CreateConnection();

            con.Open();

            using var com = new SQLiteCommand(sql, con);

            com.Parameters.Add("@id", DbType.Int32).Value = playlist.ID;

            com.ExecuteNonQuery();
        }

        private static Playlist ReadPlaylist(SQLiteDataReader reader, long? id = null)
        {
            long plId = id ?? reader.GetInt64(COL_ID);
            string name = reader.GetString(COL_NAME);
            var songs = (System.Text.Json.JsonSerializer.Deserialize<string[]>(reader.GetString(COL_SONGS)) ?? Array.Empty<string>()).ToList();

            byte[] picture;

            using (var blob = reader.GetBlob(COL_COVER, true))
            {
                var bL = blob.GetCount();

                picture = new byte[bL];
            }

            reader.GetBytes(COL_COVER, 0, picture, 0, picture.Length);

            return new Playlist(plId, name, songs, picture);
        }
    }
}
