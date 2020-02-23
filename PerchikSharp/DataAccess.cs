using PerchikSharp.Db;
using PersikSharp.Tables;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PersikSharp
{
    public class PerschikDB
    {
        SQLiteAsyncConnection db = null;

        public PerschikDB(string path)
        {
            this.db = new SQLiteAsyncConnection(path);
        }

        public void Create()
        {
            db.CreateTableAsync<DbUser>();
            db.CreateTableAsync<DbMessage>();
            db.CreateTableAsync<DbRestriction>();
        }

        public Task<List<T>> GetRowsByFilterAsync<T>(Expression<Func<T, bool>> filter) where T : class, ITable, new()
        {
            if (db == null)
                throw new NullReferenceException();

            return db.Table<T>().Where(filter).ToListAsync();
        }

        public Task<T> GetRowsByIdAsync<T>(int Id, string table) where T : class, ITable, new()
        {
            if (db == null)
                throw new NullReferenceException();

            return Task.Run(async () =>
            {
                var objects = await db.QueryAsync<T>($"select * from {table} where Id={Id}");
                if (objects != null)
                    return objects.Capacity != 0 ? objects[0] : null;
                else
                    return null;
            });
        }

        public List<T> GetRows<T>() where T : ITable, new()
        {
            if (db == null)
                throw new NullReferenceException();

            return db.Table<T>().ToListAsync().Result;
        }

        public bool isUserExist(DbUser user)
        {
            if (db == null)
                throw new NullReferenceException();

            return GetRowsByFilterAsync<DbUser>(a => a.Id == user.Id).Result.Count != 0;
        }

        public Task InsertRowAsync(object user)
        {
            if (db == null)
                throw new NullReferenceException();

            return db.InsertAsync(user);
        }

        public Task InsertOrReplaceRowAsync(object user)
        {
            if (db == null)
                throw new NullReferenceException();

            return db.InsertOrReplaceAsync(user);
        }

        public Task<int> UpdateColumnByIdAsync(int Id, object value, string table, string coulumn)
        {
            if (db == null)
                throw new NullReferenceException();

            return db.ExecuteAsync($"update {table} set {coulumn}={value} where Id={Id}");
        }

        public Task<int> ExecuteAsync(string command, params object[] ps)
        {
            if (db == null)
                throw new NullReferenceException();

            return db.ExecuteAsync(command, ps);
        }

        public Task<List<T>> ExecuteQueryAsync<T>(string command, params object[] ps) where T: ITable, new()
        {
            if (db == null)
                throw new NullReferenceException();

            return db.QueryAsync<T>(command, ps);
        }

        public Task<T> ExecuteScalarAsync<T>(string command, params object[] ps)
        {
            if (db == null)
                throw new NullReferenceException();

            return db.ExecuteScalarAsync<T>(command, ps);
        }

        public void InsertUserIfExist(DbUser user)
        {
            if (db == null)
                throw new NullReferenceException();

            if (isUserExist(user))
                InsertRowAsync(user);
        }

        public Task RemoveRowByIdAsync<T>(int Id) where T: class, ITable, new()
        {
            if (db == null)
                throw new NullReferenceException();

            return Task.Run(async () =>
            {
                var users = await GetRowsByFilterAsync<T>(a => a.Id == Id);
                if (users.Count == 0)
                    throw new KeyNotFoundException();

                await db.DeleteAsync(users[0]);
            });
        }

        public Task AddRestrictionAsync(DbUser user, long chatId, int forSecond)
        {
            return Task.Run(async () =>
            {
                await InsertRowAsync(new DbRestriction()
                {
                    UserId = user.Id,
                    ChatId = chatId.ToString(),
                    DateTimeFrom = DbConverter.DateTimeUTC2.ToString("yyyy-MM-dd HH:mm:ss"),
                    DateTimeTo = DbConverter.DateTimeUTC2.AddSeconds(forSecond).ToString("yyyy-MM-dd HH:mm:ss")
                });

                var _user = user;
                _user.RestrictionId = ExecuteScalarAsync<int>("select seq from sqlite_sequence where name='Restrictions'").Result;
                await InsertOrReplaceRowAsync(_user);
            });
        }

        public DbUser[] FindUser(string search_str)
        {
            string upper_name = search_str.ToUpper();

            var all_users = this.GetRows<DbUser>();
            var users = all_users.Where(u =>
            {
                if (u.FirstName != null && u.FirstName.ToUpper().Contains(upper_name))
                    return true;
                if (u.LastName != null && u.LastName.ToUpper().Contains(upper_name))
                    return true;
                if (u.Username != null && u.Username.ToUpper().Contains(upper_name))
                    return true;

                return false;
            });

            if (users.Count() == 0)
                return null;
            else
                return users.ToArray();
        }
    } 
}
