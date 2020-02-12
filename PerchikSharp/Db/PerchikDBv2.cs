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
    class PerchikDBv2 : DbContext
    {
        public DbSet<Tables.Userv2> Users { get; set; }
        public DbSet<Tables.Messagev2> Messages { get; set; }
        public DbSet<Tables.Restrictionv2> Restrictions { get; set; }
        public DbSet<Tables.Chatv2> Chats { get; set; }
        public DbSet<Tables.ChatUserv2> ChatUsers { get; set; }


        private object _lock = new object();
        public PerchikDBv2()
        {
            Database.EnsureCreated();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tables.ChatUserv2>()
                .HasKey(t => new { t.ChatId, t.UserId });

            modelBuilder.Entity<Tables.ChatUserv2>()
                .HasOne(cu => cu.Chat)
                .WithMany(c => c.ChatUsers)
                .HasForeignKey(cu => cu.ChatId);

            modelBuilder.Entity<Tables.ChatUserv2>()
                .HasOne(cu => cu.User)
                .WithMany(c => c.ChatUsers)
                .HasForeignKey(cu => cu.UserId);

            modelBuilder.Entity<Tables.Messagev2>()
                .HasKey(x => new { x.Id, x.Date});

            modelBuilder.Entity<Tables.Messagev2>()
                .Property(x => x.Id)
                .ValueGeneratedOnAdd();
        }
        static public PerchikDBv2 Context { 
            get {
                var obj = new PerchikDBv2();
                //obj.GetService<ILoggerFactory>().AddProvider(new DbLoggerProvider());
                return obj;
            }
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //StringManager.
            optionsBuilder.UseMySql("server=192.168.1.202;UserId=pieprz;Password=AvtoBot12;database=pieprz;");
            //optionsBuilder.EnableSensitiveDataLogging();
        }

        public void AddOrUpdateUser(Tables.Userv2 user, long chatId)
        {
            lock (_lock)
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
                    this.ChatUsers.Add(new Tables.ChatUserv2()
                    {
                        ChatId = chatId,
                        UserId = user.Id
                    });
                }
                this.SaveChanges();
            }
        }

        public void UpdateUser(Tables.Userv2 user)
        {
            lock (_lock)
            {
                var existingUser = this.Users.Where(x => x.Id == user.Id).FirstOrDefault();
                if (existingUser != null)
                {
                    this.Entry(existingUser).State = EntityState.Detached;
                    this.Entry(user).State = EntityState.Modified;
                }
                this.SaveChanges();
            }
        }

        public int AddMessage(Tables.Messagev2 msg)
        {
            lock (_lock)
            {
                this.Messages.Add(msg);
                this.SaveChanges();
                return msg.Id;
            }
            //this.ChatMessages.Add(new Tables.ChatMessagev2()
            //{
            //    ChatId = msg.ChatId,
            //    MessageId = msg.Id
            //});
        }

        public void AddOrUpdateChat(Tables.Chatv2 chat)
        {
            lock (_lock)
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
