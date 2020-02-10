using System;
using System.Collections.Generic;
using System.Text;

namespace PerchikSharp.Db.Tables
{
    class ChatMessagev2
    {
        public int Id { get; set; }
        public long ChatId { get; set; }
        public Chatv2 Chat { get; set; }
        public int MessageId { get; set; }
        public Messagev2 Message { get; set; }
    }
}
