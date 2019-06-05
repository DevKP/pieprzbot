using SQLite;
using System;

namespace PersikSharp
{
    [Table("Messages")]
    public partial class DbMessage : ITable
    {
        [PrimaryKey, Column("Id")]
        public Int32 Id { get; set; }

        [Column("UserId")]
        public Int32 UserId { get; set; }

        [Column("Text"), MaxLength(4096)]
        public String Text { get; set; }

        [Column("DateTime"), MaxLength(30)]
        public String DateTime { get; set; }
    }
}
