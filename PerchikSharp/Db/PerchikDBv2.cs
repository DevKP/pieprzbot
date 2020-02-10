using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerchikSharp.Db
{
    class PerchikDBv2 : DbContext
    {
        public DbSet<Tables.Userv2> Users { get; set; }
        public DbSet<Tables.Messagev2> Messages { get; set; }
        public DbSet<Tables.Restrictionv2> Restrictions { get; set; }
        public DbSet<Tables.Chatv2> Chats { get; set; }
        public PerchikDBv2()
        {
            //Database.EnsureDeleted();
            Database.EnsureCreated();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }
        static public PerchikDBv2 Context { get { return new PerchikDBv2(); } }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql("server=localhost;UserId=root;Password=AvtoBot12;database=pieprz;");

        }
        public void AddOrUpdateUser(Tables.Userv2 user)
        {
            
            var existingUser = this.Users.Where(x => x.Id == user.Id).FirstOrDefault();
            if (existingUser != null)
            {
                this.Entry(existingUser).State = EntityState.Detached;
                this.Entry(user).State = EntityState.Modified;
            }
            else
            {
                this.Users.Add(user);
            }
            this.SaveChanges();
        }
        public int AddMessage(Tables.Messagev2 msg)
        {
            this.Messages.Add(msg);
            this.SaveChanges();
            return msg.Id;
            //this.ChatMessages.Add(new Tables.ChatMessagev2()
            //{
            //    ChatId = msg.ChatId,
            //    MessageId = msg.Id
            //});
        }

        public void AddOrUpdateChat(Tables.Chatv2 chat)
        {

            var existingChat = this.Chats.Where(c => c.Id == chat.Id).FirstOrDefault();
            if (existingChat != null)
            {
                this.Entry(existingChat).State = EntityState.Detached;
                this.Entry(chat).State = EntityState.Modified;
            }
            else
            {
                this.Chats.Add(chat);
            }
            this.SaveChanges();
        }

        //public void InsertOrUpdate<TEnity>(DbContext context, DbSet entity)
        //{
        //    context.Entry(entity).State = entity.Id == 0 ?
        //                                   EntityState.Added :
        //                                   EntityState.Modified;
        //    context.SaveChanges();
        //}
    }
}
