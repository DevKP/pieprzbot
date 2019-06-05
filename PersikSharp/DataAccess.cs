//using SQLite;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PersikSharp
{
    public class SQLiteDb
    {
        SQLiteConnection db;

        public SQLiteDb(string path)
        {
            this.db = new SQLiteConnection(path);
        }

        public void Create()
        {
            db.CreateTable<DbUser>();
            db.CreateTable<DbMessage>();

        }

        public Task<T> GetRowByFilterAsync<T>(Expression<Func<T, bool>> filter) where T : class, ITable, new()
        {
            return Task.Run(() => db.Table<T>().FirstOrDefault(filter));
        }

        public Task<T> GetRowByIdAsync<T>(int Id, string table) where T : class, ITable
        {
            return Task.Run(() =>
            {
                var objects = db.CreateCommand($"select * from {table} where Id={Id}").ExecuteQuery<T>();
                if (objects != null)
                    return objects.Capacity != 0 ? objects[0] : null;
                else
                    return null;
            });
        }

        public List<T> GetRows<T>() where T : ITable, new()
        {
            return db.Table<T>().ToList();
        }

        public bool isUserExist(DbUser user)
        {
            return GetRowByIdAsync<DbUser>(user.Id, "Users") != null;
        }

        public Task InsertRowAsync(object user)
        {
            return Task.Run(() => db.InsertOrReplace(user));
        }

        public Task UpdateColumnByIdAsync(int Id, object value, string table, string coulumn)
        {
            return Task.Run(() => db.CreateCommand($"update {table} set {coulumn}={value} where Id={Id}").ExecuteNonQuery());
        }

        public Task ExecuteNonQueryAsync(string command, params object[] ps)
        {
            return Task.Run(() => db.CreateCommand(command, ps).ExecuteNonQuery());
        }

        public Task<List<T>> ExecuteQueryAsync<T>(string command, params object[] ps) where T: ITable
        {
            return Task.Run(() => db.CreateCommand(command, ps).ExecuteQuery<T>());
        }

        public void InsertUserIfExist(DbUser user)
        {
            if (isUserExist(user))
                InsertRowAsync(user);
        }

        public Task RemoveRowByIdAsync<T>(int Id) where T: class, ITable
        {
            return Task.Run(() =>
            {
                var user = GetRowByIdAsync<T>(Id, "Users").Result;
                db.Delete(user);
            });
        }

        public Task RemoveUserAsync(int Id)
        {
            return Task.Run(() =>
            {
                var user = GetRowByIdAsync<DbUser>(Id, "Users").Result;
                db.Delete(user);
            });
        }
    }

    public interface ITable
    {
        [PrimaryKey, Column("Id")]
        Int32 Id { get; set; }
    }

    [Table("Messages")]
    public partial class DbMessage : ITable
    {
        [PrimaryKey, Column("Id")]
        public Int32 Id { get; set; }

        [Column("UserId")]
        public Int32 UserId { get; set; }

        [Column("Text"), MaxLength(4096)]
        public String Text { get; set; }

        [Column("DateTime"), MaxLength(30)]
        public String DateTime { get; set; }
    }

    [Table("Users")]
    public partial class DbUser : ITable
    {
        [PrimaryKey, Column("Id")]
        public Int32 Id { get; set; }

        [Column("FirstName"), MaxLength(30)]
        public String FirstName { get; set; }

        [Column("LastName"), MaxLength(30)]
        public String LastName { get; set; }

        [Column("Username"), MaxLength(30)]
        public String Username { get; set; }

        [Column("LastMessage"), MaxLength(30)]
        public String LastMessage { get; set; }

        [Column("Restricted")]
        public Boolean Restricted { get; set; } = false;
    }
    
}
