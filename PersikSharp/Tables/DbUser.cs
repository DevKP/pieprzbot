using SQLite;
using System;

namespace PersikSharp
{
    [Table("Users")]
    public partial class DbUser : ITable
    {
        [PrimaryKey, Column("Id")]
        public Int32 Id { get; set; }

        [Column("FirstName"), MaxLength(30)]
        public String FirstName { get; set; }

        [Column("LastName"), MaxLength(30)]
        public String LastName { get; set; }

        [Column("Username"), MaxLength(30)]
        public String Username { get; set; }

        [Column("LastMessage"), MaxLength(30)]
        public String LastMessage { get; set; }

        [Column("Restricted")]
        public Boolean Restricted { get; set; } = false;
    }
}
