using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;

namespace PerchikSharp.Db
{
    class PerchikDB : LiteDatabase//(c)
    {
        public ILiteCollection<Tables.User> UserCollFactory 
        {
            get { return this.GetCollection<Tables.User>("Users"); } 
        }
        public ILiteCollection<Tables.Chat> ChatCollFactory
        {
            get { return this.GetCollection<Tables.Chat>("Chats"); }
        }
        public ILiteCollection<Tables.Message> MessageCollFactory
        {
            get { return this.GetCollection<Tables.Message>("Messages"); }
        }
        public ILiteCollection<Tables.ChatMessage> ChatMessageCollFactory
        {
            get { return this.GetCollection<Tables.ChatMessage>("ChatMessages"); }
        }
        public ILiteCollection<Tables.Restriction> BanCollFactory
        {
            get { return this.GetCollection<Tables.Restriction>("Restrictions"); }
        }

        public PerchikDB(string connectionString) : base(connectionString)
        {
        }
       
        public List<Tables.Chat> FindChat(Expression<Func<Tables.Chat, bool>> predicate)
        {
            var col = this.GetCollection<Tables.Chat>("Chats");
            return col.Find(predicate).ToList();
        }
        public bool UpsertChat(Tables.Chat chat)
        {
            var col = this.GetCollection<Tables.Chat>("Chats");
            return col.Upsert(chat);
        }
        public List<Tables.User> FindUser(Expression<Func<Tables.User, bool>> predicate)
        {
            var col = this.GetCollection<Tables.User>("Users");
            return col.Find(predicate).ToList();
        }
        public bool UpsertUser(Tables.User user)
        {
            var col = this.GetCollection<Tables.User>("Users");
            return col.Upsert(user);
        }
        public List<Tables.Message> FindMessage(Expression<Func<Tables.Message, bool>> predicate)
        {
            var col = this.GetCollection<Tables.Message>("Messages");
            return col.Find(predicate).ToList();
        }
        public bool UpsertMessage(Tables.Message msg)
        {
            var col = this.GetCollection<Tables.Message>("Messages");
            return col.Upsert(msg);
        }

        public void AddMessageToChat(long chatid, Tables.Message message)
        {
            if (message.from == null || message.date == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            var mCollection = this.MessageCollFactory;
            var cmCollection = this.ChatMessageCollFactory;
            var chatmessage = new Tables.ChatMessage()
            {
                chat = new Tables.Chat() { id = chatid },
                message = message
            };
            mCollection.Insert(message);
            cmCollection.Insert(chatmessage);
        }
        public List<Tables.Restriction> FindRestriction(Expression<Func<Tables.Restriction, bool>> predicate)
        {
            var col = this.GetCollection<Tables.Restriction>("Restrictions");
            return col.Find(predicate).ToList();
        }
        public bool UpsertRestriction(Tables.Restriction restriction)
        {
            var col = this.GetCollection<Tables.Restriction>("Restrictions");
            return col.Upsert(restriction);
        }
        public bool AddRestriction(Tables.Restriction restriction)
        {
            if(restriction.chat == null || restriction.user == null ||
                restriction.date == null || restriction.until == null)
            {
                throw new ArgumentNullException(nameof(restriction));
            }
            var bCollection = this.BanCollFactory;
            var uCollection = this.UserCollFactory;
            var users = uCollection.Find(u => u.id == restriction.user.id);
            if (users.Count() > 0)
            {
                var user = users.First();
                user.restriction = restriction;
                bCollection.Insert(restriction);
                uCollection.Update(user);
                return true;
            }
            else
                return false;
        }

    }
}
