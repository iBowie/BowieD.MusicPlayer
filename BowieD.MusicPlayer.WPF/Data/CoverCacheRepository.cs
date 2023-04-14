using BowieD.MusicPlayer.WPF.Common;
using BowieD.MusicPlayer.WPF.Extensions;
using BowieD.MusicPlayer.WPF.Models;
using System.Data;
using System.IO;

namespace BowieD.MusicPlayer.WPF.Data
{
    public sealed class CoverCacheRepository : BaseRepository
    {
        #region BOILER PLATE
        private static CoverCacheRepository? _instance;
        public static CoverCacheRepository Instance
        {
            get
            {
                if (_instance is null)
                {
                    _instance = new CoverCacheRepository();
                }

                return _instance;
            }
        }

        private const long CURRENT_VERSION = 1;
        private const string
            TABLE_NAME = "album_cover_cache",
            COL_ID = "id",
            COL_ALBUM = "album",
            COL_COVER = "cover";

        public CoverCacheRepository() : base(Path.Combine(DataFolder.DataDirectory, "albumCoverCache.db")) { }

        protected override void OnPrepare()
        {
            ExecuteNonQuery($"CREATE TABLE IF NOT EXISTS {TABLE_NAME} (" +
                $"{COL_ID} INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, " +
                $"{COL_ALBUM} VARCHAR(128), " +
                $"{COL_COVER} BLOB" +
                $")");

            var version = (long)ExecuteScalar("PRAGMA user_version");

            // migrations here

            ExecuteNonQuery($"PRAGMA user_version = {CURRENT_VERSION}");
        }
        #endregion

        public byte[] GetOrCreateCoverFor(Album album)
        {
            string sql = $"SELECT {COL_COVER} FROM {TABLE_NAME} WHERE {COL_ALBUM} = @album LIMIT 1;";

            using var con = CreateConnection();

            con.Open();

            using var com = con.CreateCommand();
            com.CommandText = sql;
            com.Parameters.Add("@album", DbType.String).Value = album.Name;

            using var reader = com.ExecuteReader(CommandBehavior.KeyInfo);

            if (reader.HasRows && reader.Read())
            {
                byte[] picture;

                using (var blob = reader.GetBlob(COL_COVER, true))
                {
                    int picLength = blob.GetCount();

                    picture = new byte[picLength];
                }

                reader.GetBytes(COL_COVER, 0, picture, 0, picture.Length);

                return picture;
            }

            var songs = album.Songs;

            var art = CoverAnalyzer.GenerateCoverArt(songs, true);

            SetCover(album, art);

            return art;
        }

        private void SetCover(Album album, byte[] data)
        {
            string sql = $"INSERT INTO {TABLE_NAME} " +
                $"({COL_ALBUM},{COL_COVER}) " +
                $"values(@album,@cover)";

            using var con = CreateConnection();
            con.Open();
            using var com = con.CreateCommand();
            com.CommandText = sql;
            com.Parameters.Add("@album", DbType.String).Value = album.Name;
            com.Parameters.Add(@"cover", DbType.Binary).Value = data;

            com.ExecuteNonQuery();
        }

        public void ClearCovers()
        {
            string sql = $"DELETE FROM {TABLE_NAME}";

            using var con = CreateConnection();
            con.Open();
            using var com = con.CreateCommand();
            com.CommandText = sql;

            com.ExecuteNonQuery();
        }
    }
}
