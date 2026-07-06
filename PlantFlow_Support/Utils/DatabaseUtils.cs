using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace PlantFlow_Support
{
    public static class DatabaseUtils
    {
        public static List<string> GetTableNames(string db_path)
        {
            List<string> tableNames = new List<string>();
            using (SQLiteConnection sqLiteConnection = new SQLiteConnection(db_path))
            {
                sqLiteConnection.Open();
                System.Data.DataTable schema = ((System.Data.Common.DbConnection)sqLiteConnection).GetSchema("Tables");
                foreach (DataRow row in schema.Rows)
                    tableNames.Add(row["TABLE_NAME"].ToString());
            }
            return tableNames;
        }

        public static System.Data.DataTable GetDataTable(SQLiteConnection connection, string sql)
        {
            System.Data.DataTable dataTable = new System.Data.DataTable();
            using (SQLiteCommand sqLiteCommand = new SQLiteCommand(sql, connection))
            {
                using (SQLiteDataReader reader = sqLiteCommand.ExecuteReader())
                    dataTable.Load((IDataReader)reader);
            }
            return dataTable;
        }

        public static void InsertNewTable(string db_path, string table_name)
        {
            using (SQLiteConnection sqLiteConnection = new SQLiteConnection(db_path))
            {
                sqLiteConnection.Open();
                using (SQLiteCommand sqLiteCommand = new SQLiteCommand(sqLiteConnection))
                {
                    sqLiteCommand.CommandText = "CREATE TABLE IF NOT EXISTS\"" + table_name + "\" (\"Revision\" TEXT, \"Description\" TEXT, \"Date\" TEXT, \"DrawBy\" TEXT,\"CheckedByA\" TEXT,\"CheckedByB\" TEXT,\"ApprovedBy\" TEXT)";
                    sqLiteCommand.ExecuteNonQuery();
                }
            }
        }

        public static void RenameTable(string db_path, string old_name, string new_name)
        {
            using (SQLiteConnection sqLiteConnection = new SQLiteConnection(db_path))
            {
                sqLiteConnection.Open();
                using (SQLiteCommand sqLiteCommand = new SQLiteCommand(sqLiteConnection))
                {
                    sqLiteCommand.CommandText = "ALTER TABLE \"" + old_name + "\" RENAME TO \"" + new_name + "\"";
                    sqLiteCommand.ExecuteNonQuery();
                }
            }
        }

        public static void InsertNewRecord(string db_path, string table_name, string rev, string desc, string date, string dwn, string checked1, string checked2, string approved)
        {
            using (SQLiteConnection sqLiteConnection = new SQLiteConnection(db_path))
            {
                sqLiteConnection.Open();
                using (SQLiteCommand sqLiteCommand = new SQLiteCommand(sqLiteConnection))
                {
                    sqLiteCommand.CommandText = "INSERT INTO \"" + table_name + "\" (Revision, Description, Date, DrawBy, CheckedByA, CheckedByB, ApprovedBy) VALUES (@Value1, @Value2, @Value3, @Value4, @Value5, @Value6, @Value7)";
                    sqLiteCommand.Parameters.AddWithValue("@Value1", (object)rev);
                    sqLiteCommand.Parameters.AddWithValue("@Value2", (object)desc);
                    sqLiteCommand.Parameters.AddWithValue("@Value3", (object)date);
                    sqLiteCommand.Parameters.AddWithValue("@Value4", (object)dwn);
                    sqLiteCommand.Parameters.AddWithValue("@Value5", (object)checked1);
                    sqLiteCommand.Parameters.AddWithValue("@Value6", (object)checked2);
                    sqLiteCommand.Parameters.AddWithValue("@Value7", (object)approved);
                    sqLiteCommand.ExecuteNonQuery();
                }
            }
        }

        public static void UpdateRecord(string db_path, string table_name, string rev, string desc, string date, string dwn, string checked1, string checked2, string approved)
        {
            using (SQLiteConnection sqLiteConnection = new SQLiteConnection(db_path))
            {
                sqLiteConnection.Open();
                using (SQLiteCommand sqLiteCommand = new SQLiteCommand(sqLiteConnection))
                {
                    sqLiteCommand.CommandText = "UPDATE \"" + table_name + "\" SET Revision = @Value1, Description = @Value2, Date = @Value3, DrawBy = @Value4, CheckedByA = @Value5, CheckedByB = @Value6, ApprovedBy = @Value7 WHERE Revision = @Value1";
                    sqLiteCommand.Parameters.AddWithValue("@Value1", (object)rev);
                    sqLiteCommand.Parameters.AddWithValue("@Value2", (object)desc);
                    sqLiteCommand.Parameters.AddWithValue("@Value3", (object)date);
                    sqLiteCommand.Parameters.AddWithValue("@Value4", (object)dwn);
                    sqLiteCommand.Parameters.AddWithValue("@Value5", (object)checked1);
                    sqLiteCommand.Parameters.AddWithValue("@Value6", (object)checked2);
                    sqLiteCommand.Parameters.AddWithValue("@Value7", (object)approved);
                    sqLiteCommand.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteRecord(string db_path, string table_name, string revision)
        {
            using (SQLiteConnection sqLiteConnection = new SQLiteConnection(db_path))
            {
                sqLiteConnection.Open();
                using (SQLiteCommand sqLiteCommand = new SQLiteCommand(sqLiteConnection))
                {
                    sqLiteCommand.CommandText = "DELETE FROM \"" + table_name + "\" WHERE Revision = @Value1";
                    sqLiteCommand.Parameters.AddWithValue("@Value1", (object)revision);
                    sqLiteCommand.ExecuteNonQuery();
                }
            }
        }
    }
}
