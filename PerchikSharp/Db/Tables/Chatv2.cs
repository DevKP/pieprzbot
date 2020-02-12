using System;
using System.Collections.Generic;
using System.Text;

namespace PerchikSharp.Db.Tables
{
    class Chatv2
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<Messagev2> Messages { get; set; }
        public List<ChatUserv2> ChatUsers { get; set; }

        public Chatv2()
        {
            ChatUsers = new List<ChatUserv2>();
            Messages = new List<Messagev2>();
        }
    }
}
