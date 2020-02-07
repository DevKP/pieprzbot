using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace PerchikSharp.Db.Tables
{
    class Restriction
    {
        [BsonId]
        public int id { get; set; }
        [BsonRef("Chats")]
        public Chat chat { get; set; }
        public DateTime date { get; set; }
        public DateTime until { get; set; }
        [BsonRef("Users")]
        public User user { get; set; }
    }
}
