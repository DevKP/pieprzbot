﻿using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersikSharp.Tables
{
    public interface ITable
    {
        [PrimaryKey, Column("Id")]
        Int32 Id { get; set; }
    }
}
