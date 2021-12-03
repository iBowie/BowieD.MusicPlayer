using System.Data.SQLite;

namespace BowieD.MusicPlayer.WPF.Extensions
{
    public static class SQLiteDataReaderExtension
    {
        public static SQLiteBlob GetBlob(this SQLiteDataReader reader, string columnName, bool readOnly)
        {
            var ordinal = reader.GetOrdinal(columnName);

            return reader.GetBlob(ordinal, readOnly);
        }
    }
}
