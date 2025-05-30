using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TimeTable_Project
{
    public class VacationType
    {
        public string name{get; set;}
        public Table_codes table_code {get; set;}

        public static int GetVacationType(string textMsg)
        {
            int vacationTypeId = 0;

            if (textMsg == "/Office")
            {
                vacationTypeId = 10;

            }
            else if (textMsg == "/RemoteWork")
            {
                vacationTypeId = 5;
            }
            else if (textMsg == "/Family")
            {
                vacationTypeId = 4;
            }
            else if (textMsg == "/BusinessTrip")
            {
                vacationTypeId = 6;
            }
            else if (textMsg == "/OnStudy")
            {
                vacationTypeId = 8;
            }
            else if (textMsg == "/Vacation")
            {
                vacationTypeId = 1;
            }
            else if (textMsg == "/Medical")
            {
                vacationTypeId = 3;
            }
            else
            {
                vacationTypeId = 11;
            }

            return vacationTypeId;
        }

        public static string GetVacationTypeByID(int id)
        {
            string query = $"SELECT id, name, table_code FROM public.vacation_types Where id={id};";
            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                var vacationType = connect.QueryFirstOrDefault<VacationType>(query);
                if (vacationType != null)
                {
                    return $"{vacationType.name} ({vacationType.table_code})";
                }


                return "";
            }
        }
    }
}
