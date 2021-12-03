using System.Data;
using System.Data.SQLite;
using System.IO;

namespace BowieD.MusicPlayer.WPF.Data
{
    public abstract class BaseRepository
    {
        private readonly string _dbFileName;

        public BaseRepository(string dbFileName)
        {
            _dbFileName = dbFileName;

            Prepare();
        }

        protected SQLiteConnection CreateConnection()
        {
            return new SQLiteConnection($"Data Source={_dbFileName}; Version=3;");
        }

        private void Prepare()
        {
            if (!File.Exists(_dbFileName))
            {
                SQLiteConnection.CreateFile(_dbFileName);
            }

            OnPrepare();
        }

        protected abstract void OnPrepare();

        protected int ExecuteNonQuery(string sql, CommandBehavior behavior = CommandBehavior.Default)
        {
            using var con = CreateConnection();

            con.Open();

            using var com = new SQLiteCommand(sql, con);

            return com.ExecuteNonQuery(behavior);
        }

        protected object ExecuteScalar(string sql, CommandBehavior behavior = CommandBehavior.Default)
        {
            using var con = CreateConnection();

            con.Open();

            using var com = new SQLiteCommand(sql, con);

            return com.ExecuteScalar(behavior);
        }
    }
}
