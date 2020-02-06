using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace PerchikSharp.Db.Tables
{
    class User
    {
        [BsonId]
        public int id { get; set; }
        public string firstname { get; set; }
        public string? lastname { get; set; }
        public string? username { get; set; }
        public bool restricted { get; set; }
        [BsonRef("Restrictions")]
        public List<Restriction>? restrictions { get; set; }
    }
}
