using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersikSharp.Tables
{
    [Table("Restrictions")]
    public partial class DbRestriction : ITable
    {
        [PrimaryKey, AutoIncrement, Column("Id")]
        public Int32 Id { get; set; }

        [Column("UserId")]
        public Int32 UserId { get; set; }

        [Column("ChatId")]
        public String ChatId { get; set; }

        [Column("DateTimeFrom"), MaxLength(30)]
        public String DateTimeFrom { get; set; }

        [Column("DateTimeTo"), MaxLength(30)]
        public String DateTimeTo { get; set; }
    }
}
