using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace PersikSharp
{
    class DataBaseHelper
    {
        SQLiteConnection sqlite_conn;
        public DataBaseHelper()
        {
            sqlite_conn = CreateConnection();
            CreateTable();
        }

        static SQLiteConnection CreateConnection()
        {
            SQLiteConnectionStringBuilder builder = new SQLiteConnectionStringBuilder();
            builder.DataSource = "database.db";
            SQLiteConnection sqlite_conn;
            sqlite_conn = new SQLiteConnection(builder.ConnectionString);
            try
            {
                sqlite_conn.Open();
            }
            catch (Exception ex)
            {

            }
            return sqlite_conn;
        }
        public void CreateTable()
        {
            string Createsql = "CREATE TABLE IF NOT EXISTS SampleTable(ID CHAR(10) PRIMARY KEY, name CHAR(30))";
            SQLiteCommand sqlite_cmd = new SQLiteCommand(Createsql, sqlite_conn);
            sqlite_cmd.ExecuteNonQuery();

            Createsql = "CREATE TABLE IF NOT EXISTS Users(Id INT PRIMARY KEY, FirstName CHAR(30), LastName CHAR(30), Username CHAR(30))";
            sqlite_cmd = new SQLiteCommand(Createsql, sqlite_conn);
            sqlite_cmd.ExecuteNonQuery();
        }

        public void AddUserIfNotExist(User user)
        {
            try
            {
                SQLiteCommand insertSQL = new SQLiteCommand(sqlite_conn);
                insertSQL.Parameters.Add("@Id", DbType.String);
                insertSQL.Parameters["@Id"].Value = user.Id;
                insertSQL.Parameters.Add("@FirstName", DbType.String);
                insertSQL.Parameters["@FirstName"].Value = user.FirstName;
                insertSQL.Parameters.Add("@LastName", DbType.String);
                insertSQL.Parameters["@LastName"].Value = user.LastName;
                insertSQL.Parameters.Add("@Username", DbType.String);
                insertSQL.Parameters["@Username"].Value = user.Username;

                insertSQL.CommandText = "SELECT count(*) FROM Users WHERE Id=@Id";
                int count = Convert.ToInt32(insertSQL.ExecuteScalar());
                if (count == 0)
                {
                    insertSQL.CommandText = "INSERT INTO Users (Id, FirstName, LastName, Username) VALUES (@Id, @FirstName, @LastName, @Username)";
                    insertSQL.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public void Test(string id, string name)
        {
            try
            {
                SQLiteCommand insertSQL = new SQLiteCommand(sqlite_conn);
                insertSQL.Parameters.Add("@ID", DbType.String);
                insertSQL.Parameters["@ID"].Value = id;
                insertSQL.Parameters.Add("@name", DbType.String);
                insertSQL.Parameters["@name"].Value = name;

                insertSQL.CommandText = "SELECT count(*) FROM SampleTable WHERE ID=@ID";
                int count = Convert.ToInt32(insertSQL.ExecuteScalar());
                if (count == 0)
                {
                    insertSQL.CommandText = "INSERT INTO SampleTable (ID, name) VALUES (@ID, @name)";
                    insertSQL.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

    }
}
