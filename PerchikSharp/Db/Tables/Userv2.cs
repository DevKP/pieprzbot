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
        public List<ChatUserv2> ChatUsers { get; set; }
        public List<Messagev2> Messages { get; set; }
        public bool Restricted { get; set; }
        public List<Restrictionv2> Restrictions { get; set; }

        public Userv2()
        {
            ChatUsers = new List<ChatUserv2>();
            Restrictions = new List<Restrictionv2>();
            Messages = new List<Messagev2>();
        }
    }
}
