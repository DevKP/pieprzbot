using SQLite;
using System;
using System.Collections.Generic;

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
            db.CreateTable<SampleTable>();
            db.CreateTable<Users>();
        }

        public List<Users> GetUsersById(int Id)
        {
            return db.CreateCommand($"select * from Users where Id={Id}").ExecuteQuery<Users>();
        }

        public bool isUserExist(Users user)
        {
            var users = GetUsersById(user.Id);
            if (users.Count > 0)
                return true;
            else
                return false;
        }

        public void InsertUser(Users user)
        {
            db.Insert(user);
        }

        public void UpdateUser(Users user)
        {
            if (isUserExist(user))
            {
                db.Update(user);
            }
            else
            {
                InsertUser(user);
            }
        }

        public void UpdateUserIfExist(Users user)
        {
            if (isUserExist(user))
            {
                UpdateUser(user);
            }
        }

        public void InsertUserIfNotExist(Users user)
        {
            if (!isUserExist(user))
            {
                InsertUser(user);
            } 
        }
        public void RemoveUser(int Id)
        {
            var users = GetUsersById(Id);
            foreach(var user in users)
                db.Delete(user);
        }
    }

    public partial class SampleTable
    {
        [PrimaryKey]
        [MaxLength(10)]
        public String ID { get; set; }
        
        [MaxLength(30)]
        public String name { get; set; }
        
    }
    
    public partial class Users
    {
        [PrimaryKey]
        public Int32 Id { get; set; }
        
        [MaxLength(30)]
        public String FirstName { get; set; }
        
        [MaxLength(30)]
        public String LastName { get; set; }
        
        [MaxLength(30)]
        public String Username { get; set; }

        [MaxLength(30)]
        public String LastMessage { get; set; }

        public Boolean Restricted { get; set; }
    }
    
}
