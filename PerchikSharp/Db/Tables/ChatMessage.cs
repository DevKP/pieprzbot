using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace PerchikSharp.Db.Tables
{
    class ChatMessage
    {
        [BsonRef("Chats")]
        public Chat chat { get; set; }
        [BsonRef("Messages")]
        public Message message { get; set; }
    }
}
