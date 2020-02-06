using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Db.Tables
{
    class Chat
    {
        public int id { get; set; }
        public ChatType type { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public Message pinnedmessage { get; set; }
    }
}
