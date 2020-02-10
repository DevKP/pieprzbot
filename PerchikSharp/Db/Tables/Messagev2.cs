﻿using System;
using System.Collections.Generic;
using System.Text;

namespace PerchikSharp.Db.Tables
{
    class Messagev2
    {
        public int Id { get; set; }
        public int MessageId {get;set;}
        public int UserId { get; set; }
        public Userv2 User { get; set; }
        public long ChatId { get; set; }
        public Chatv2 Chat { get; set; }
        public string Text { get; set; }
        public DateTime Date { get; set; }
    }
}
