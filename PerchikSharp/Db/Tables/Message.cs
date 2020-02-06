using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Db.Tables
{
    class Message
    {
        [BsonId]
        public int id { get; set; }
        [BsonRef("Users")]
        public User from { get; set; }
        public int? replytoid { get; set; }
        public string text { get; set; }
        public MessageType type { get; set; }
        public string fileid { get; set; }
        public DateTime date { get; set; }
        public DateTime? editdate { get; set; }
    }
}
