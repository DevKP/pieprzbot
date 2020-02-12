using System;
using System.Collections.Generic;
using System.Text;

namespace PerchikSharp.Db.Tables
{
    class Chat
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<Message> Messages { get; set; }
        public List<ChatUser> ChatUsers { get; set; }

        public Chat()
        {
            ChatUsers = new List<ChatUser>();
            Messages = new List<Message>();
        }
    }
}
