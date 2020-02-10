using System;
using System.Collections.Generic;
using System.Text;

namespace PerchikSharp.Db.Tables
{
    class Restrictionv2
    {
        public int Id { get; set; }
        public long ChatId { get; set; }
        public Chatv2 Chat { get; set; }
        public DateTime Date { get; set; }
        public DateTime Until { get; set; }
    }
}
