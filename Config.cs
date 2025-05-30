using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using Dapper;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Data;

namespace TimeTable_Project
{
    public static class Config
    {
        public static string SQLConnectionString = "Host=localhost;Port=5432;Database=TimeTable;Username=postgres;Password=123456;";

    }
}
