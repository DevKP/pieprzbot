using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Db.Tables
{
    class Chat
    {
        [BsonId]
        public long id { get; set; }
        public ChatType type { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        [BsonRef("Messages")]
        public Message pinnedmessage { get; set; }
    }
}
