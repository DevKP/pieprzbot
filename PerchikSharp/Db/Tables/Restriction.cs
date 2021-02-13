using System;

namespace PerchikSharp.Db.Tables
{
    class Restriction
    {
        public int Id { get; set; }
        public long ChatId { get; set; }
        public Chat Chat { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public DateTime Date { get; set; }
        public DateTime Until { get; set; }
    }
}
