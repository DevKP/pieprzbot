using System;
using System.Collections.Generic;
using System.Text;

namespace PerchikSharp.Db.Tables
{
    class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Description { get; set; }
        public List<ChatUser> ChatUsers { get; set; }
        public List<Message> Messages { get; set; }
        public bool Restricted { get; set; }
        public List<Restriction> Restrictions { get; set; }
        public List<Pidr> Pidrs { get; set; }

        public User()
        {
            ChatUsers = new List<ChatUser>();
            Restrictions = new List<Restriction>();
            Messages = new List<Message>();
            Pidrs = new List<Pidr>();
        }
    }
}
