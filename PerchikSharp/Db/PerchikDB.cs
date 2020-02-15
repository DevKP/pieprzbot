using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerchikSharp.Db
{
    class PerchikDB : DbContext
    {
        public DbSet<Tables.User> Users { get; set; }
        public DbSet<Tables.Message> Messages { get; set; }
        public DbSet<Tables.Restriction> Restrictions { get; set; }
        public DbSet<Tables.Chat> Chats { get; set; }
        public DbSet<Tables.ChatUser> ChatUsers { get; set; }

        public static string ConnectionString { get; set; }

        public PerchikDB()
        {
            Database.EnsureCreated();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tables.ChatUser>()
                .HasKey(t => new { t.ChatId, t.UserId });

            modelBuilder.Entity<Tables.ChatUser>()
                .HasOne(cu => cu.Chat)
                .WithMany(c => c.ChatUsers)
                .HasForeignKey(cu => cu.ChatId);

            modelBuilder.Entity<Tables.ChatUser>()
                .HasOne(cu => cu.User)
                .WithMany(c => c.ChatUsers)
                .HasForeignKey(cu => cu.UserId);

            modelBuilder.Entity<Tables.Message>()
                .HasKey(x => new { x.Id, x.Date});

            modelBuilder.Entity<Tables.Message>()
                .Property(x => x.Id)
                .ValueGeneratedOnAdd();
        }
        static public PerchikDB Context { 
            get {
                var obj = new PerchikDB();
                obj.GetService<ILoggerFactory>().AddProvider(new DbLoggerProvider());
                return obj;
            }
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(ConnectionString);
            optionsBuilder.EnableSensitiveDataLogging();
        }

        public void UpdateUser(Tables.User user)
        {

            var existingUser = this.Users.Where(x => x.Id == user.Id).FirstOrDefault();
            if (existingUser != null)
            {
                this.Entry(existingUser).State = EntityState.Detached;
                this.Entry(user).State = EntityState.Modified;
            }
            this.SaveChanges();

        }

        public void AddMessage(Tables.Message msg)
        {
            this.Messages.Add(msg);
            this.SaveChanges();
        }
        public void AddRestriction(Tables.Restriction restr)
        {
            var existingUser = Users.Where(x => x.Id == restr.UserId).FirstOrDefault();
            if (existingUser != null)
            {
                Restrictions.Add(restr);
                existingUser.Restricted = true;
                SaveChanges();
            }
        }

        public void UpsertChat(Tables.Chat chat)
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
        public void UpsertUser(Tables.User user, long chatId)
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
                this.ChatUsers.Add(new Tables.ChatUser()
                {
                    ChatId = chatId,
                    UserId = user.Id
                });
            }
            this.SaveChanges();
        }
        public void UpsertMessage(Tables.Message message)
        {
            var existingMsg = this.Messages.Where(x => x.MessageId == message.MessageId).FirstOrDefault();
            if (existingMsg != null)
            {
                this.Entry(existingMsg).State = EntityState.Detached;
                this.Entry(message).State = EntityState.Modified;
            }
            else
            {
                this.Messages.Add(message);
            }
            this.SaveChanges();
        }
    }
}
