using System;
using System.Collections.Generic;
using System.Text;

namespace PerchikSharp.Db.Tables
{
    class ChatUserv2
    {
        public long ChatId { get; set; }
        public Chatv2 Chat { get; set; }
        public int UserId { get; set; }
        public Userv2 User { get; set; }
    }
}
