using Dapper;
using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Telegram.BotBuilder.Extensions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static TimeTable_Project.Program;

namespace TimeTable_Project
{
    public class StaffPerson
    {
        public int? id { get; set; }
        public string? name { get; set; }
        public string? surname { get; set; }
        public string? patronymic { get; set; }
        public StaffRules? rule { get; set; }
        public int? table_number { get; set; }
        public long? telegram_id { get; set; }
        public bool? reminder_is_necessary { get; set; }
        public DateTime? date_remind { get; set; }
        public DateTime? birthday { get; set; }


        public bool AuthenticationIsComplited() 
        {
            if (telegram_id.GetValueOrDefault()!=0 && table_number.GetValueOrDefault() != 0 && !string.IsNullOrEmpty(name))
            {
                return true;
            }
            return false;
            
        
        }

        public string GetMyStatus() 
        {
            string strReturn = "";

            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                string query = $"SELECT COALESCE(vacation_types.name,'В офисе'), COALESCE(vacation_types.table_code, 'Я') as status_code, staff.table_number,staff.name, vacation_type_id, staff.id, date_start,date_end FROM public.staff as staff LEFT JOIN public.vacations as vacations on vacations.staff_table_number=staff.table_number and not cancelled and CURRENT_DATE between date_start and date_end LEFT JOIN public.vacation_types as vacation_types on vacations.vacation_type_id=vacation_types.id WHERE table_number={table_number}";

                connect.Open();

                using NpgsqlCommand cmd = new NpgsqlCommand(query, connect);

                using NpgsqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    

                    try
                    {
                        string currentStatus = $"{reader[0].ToString()} ({reader[1].ToString()})";
                        if (date_remind < DateTime.Now.Date)
                        {
                            currentStatus = "Еще не отметился";
                        }

                        strReturn = $"Ваш статус на сегодня: {currentStatus} ";
                        Console.WriteLine($"Сотрудник {table_number} запросил свой статус на сегодня. Получил: {strReturn})");
                    }
                    catch (Exception)
                    {                     
                        strReturn = $"Ошибка, не найден статус Сотрудник {table_number}";
                        Console.WriteLine(strReturn);
                    }
                }
            }


                return strReturn;
        }

        public int? GetMyHolidayDaysCount() 
        {
            string query = $"Select sum(days_total) from (\r\nSELECT sum(vacations.date_end-current_date) as days_total FROM public.vacations \r\nas vacations INNER Join public.staff as staff on vacations.staff_table_number=staff.table_number\r\nWHERE staff.table_number={table_number} and planned and not cancelled \r\nand vacations.date_end>=current_date and current_date>=vacations.date_start \r\nGroup by staff.table_number,staff.name\r\nunion\r\nSELECT sum(days) as days_total FROM public.vacations \r\nas vacations INNER Join public.staff as staff on vacations.staff_table_number=staff.table_number\r\nWHERE staff.table_number={table_number} and planned and not cancelled \r\nand vacations.date_end>=current_date and current_date<vacations.date_start \r\nGroup by staff.table_number,staff.name)";

            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                var days = connect.QueryFirstOrDefault<int?>(query);

                if (days == null)
                { return 0; }
                else
                {
                    return days;
                }
            }


        }

        public List<Vacation> GetMyHolidayDays()
        {
            string query = $"SELECT * FROM public.vacations as vacations inner join public.staff as staff on vacations.staff_table_number=staff.table_number WHERE staff.table_number={table_number} and planned and not cancelled and vacations.date_start>date_trunc('year', now()) order by date_start";

            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                var vacations = connect.Query<Vacation>(query);

                return vacations.ToList();
            }


        }

        public VacationType? UpdateStatus(string textMsg) 
        {

            string textAnswer = textMsg.Replace("/", "");
            
            int vacationTypeId = VacationType.GetVacationType(textMsg);
            
            string addedQuery = "";
            if (vacationTypeId != 11)
            {
                addedQuery = $"UPDATE public.staff SET date_remind = '{DateTime.Now.ToString("yyyy-MM-dd")}' WHERE table_number = {table_number};";
            }

            string query = $"UPDATE public.vacations\r\n\tSET cancelled=true\r\n\tWHERE staff_table_number={table_number} and planned=false and cancelled=false and date_start>=Current_Date and date_end<=Current_Date;\r\nINSERT INTO public.vacations(\r\n\tvacation_type_id, staff_table_number, cancelled, planned, date_start, date_end, days)\r\n\tVALUES ({vacationTypeId}, {table_number}, false, false, current_date, current_date, 1); {addedQuery} Select name, table_code from vacation_types where id={vacationTypeId};";

            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                var vacationType = connect.QueryFirstOrDefault<VacationType>(query);

                if (vacationType == null)
                { return null; }
                else
                {
                  return vacationType;
                }
            }

        }

        public Vacation? UpdateStatus(int vacationTypeId, Request? request=null)
        {

            string addedQuery = "";
            if (vacationTypeId != 11)
            {
                addedQuery = $"UPDATE public.staff SET date_remind = '{DateTime.Now.ToString("yyyy-MM-dd")}' WHERE table_number = {table_number};";
            }
            string query = "";

            if (request == null)
            {
                query = $"UPDATE public.vacations SET cancelled=true WHERE staff_table_number={table_number} and planned=false and cancelled=false and date_start>=Current_Date and date_end<=Current_Date; {addedQuery} INSERT INTO public.vacations(\r\n\tvacation_type_id, staff_table_number, cancelled, planned, date_start, date_end, days)\r\n\tVALUES ({vacationTypeId}, {table_number}, false, false, current_date, current_date, 1) Returning id;";
            }
            else
            {
                query = $"UPDATE public.vacations SET cancelled=true WHERE staff_table_number={table_number} and planned=false and cancelled=false and date_start>=Current_Date and date_end<=Current_Date; {addedQuery} INSERT INTO public.vacations(\r\n\tvacation_type_id, staff_table_number, cancelled, planned, date_start, date_end, days)\r\n\tVALUES ({vacationTypeId}, {table_number}, false, false, '{request.date_start.GetValueOrDefault().ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)}', '{request.date_end.GetValueOrDefault().ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)}', {request.days_quantity}) Returning id;";
            
            }
                using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                var vacation = connect.QueryFirstOrDefault<Vacation>(query);

                if (vacation == null)
                { return null; }
                else
                {
                    return vacation;
                }
            }

        }


    }
}
