//using SQLite;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

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

        public T GetRowByFilter<T>(Expression<Func<T, bool>> filter) where T : new()
        {
            return db.Table<T>().FirstOrDefault(filter);
        }

        public T GetRowById<T>(int Id, string table, string column) where T : new()
        {
            var objects = db.CreateCommand($"select * from {table} where {column}={Id}").ExecuteQuery<T>();
            if (objects != null)
                return objects[0];
            else
                return default(T);
        }

        public List<T> GetRows<T>() where T : new()
        {
            return db.Table<T>().ToList();
        }

        public bool isUserExist(DbUser user)
        {
            return GetRowById<DbUser>(user.Id, "Users", "UserId") != null;
        }

        public void InsertRow(object user)
        {
            db.InsertOrReplace(user);
        }

        public void UpdateUserIfExist(DbUser user)
        {
            if (isUserExist(user))
                InsertRow(user);
        }

        public void RemoveUser(int Id)
        {
            var user = GetRowById<DbUser>(Id, "Users", "UserId");
            db.Delete(user);
        }
    }

    public interface ITable
    {
        Int32 Id { get; set; }
    }

    [Table("Messages")]
    public partial class DbMessage : ITable
    {
        [PrimaryKey, Column("MessageId")]
        public Int32 Id { get; set; }

        public Int32 UserId { get; set; }

        [MaxLength(4096)]
        public String Text { get; set; }

        [MaxLength(30)]
        public String DateTime { get; set; }
    }

    [Table("Users")]
    public partial class DbUser : ITable
    {
        [PrimaryKey, Column("UserId")]
        public Int32 Id { get; set; }

        [MaxLength(30)]
        public String FirstName { get; set; }
        
        [MaxLength(30)]
        public String LastName { get; set; }
        
        [MaxLength(30)]
        public String Username { get; set; }

        [MaxLength(30)]
        public String LastMessage { get; set; }

        public Int32? MessagesCount { get; set; } = 0;

        public Boolean Restricted { get; set; } = false;
    }
    
}
