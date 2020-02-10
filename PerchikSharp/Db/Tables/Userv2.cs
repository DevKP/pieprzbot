using System;
using System.Collections.Generic;
using System.Text;

namespace PerchikSharp.Db.Tables
{
    class Userv2
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public long ChatId { get; set; }
        public Chatv2 Chat { get; set; }
        public int? RestrictionId { get; set; }
        public Restrictionv2 Restriction { get; set; }
    }
}
