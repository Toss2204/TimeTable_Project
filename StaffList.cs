using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using Dapper;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Telegram.Bot.Types;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Net.Http.Json;

namespace TimeTable_Project
{
    public class StaffList
    {


        public static StaffPerson CheckUserForTelegramID(long telegramId)
        {
            string query = $"SELECT * FROM public.staff WHERE telegram_id={telegramId}";


            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                var user = connect.QueryFirstOrDefault<StaffPerson>(query);

                if (user == null)
                { return null; }
                else
                {
                    return user;
                }
            }
        }

        public static StaffPerson CheckUserForTableNumber(int? tableNumber)
        {
            string query = $"SELECT * FROM public.staff WHERE table_number={tableNumber}";


            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                var user = connect.QueryFirstOrDefault<StaffPerson>(query);

                if (user == null)
                { return null; }
                else
                {
                    return user;
                }
            }
        }

        public static StaffPerson CreateUser(int tableNumber, string name, string surname, string patronymic, long telegram_id)
        {



            StaffPerson user = new StaffPerson()
            {
                table_number = tableNumber,
                name = name,
                surname = surname,
                telegram_id = telegram_id,
                patronymic = patronymic,
                rule = StaffRules.staff,
            };


            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                var sqlQuery = $"INSERT INTO public.staff(name, surname, patronymic, rule, table_number, telegram_id) VALUES (@name, @surname, @patronymic,@rule::staff_rules, @table_number, @telegram_id)  RETURNING id;";

                connect.Open();

                using NpgsqlCommand cmd = new NpgsqlCommand(sqlQuery, connect);
                cmd.Parameters.AddWithValue("name", user.name);
                cmd.Parameters.AddWithValue("surname", user.surname);
                cmd.Parameters.AddWithValue("patronymic", user.patronymic);
                cmd.Parameters.AddWithValue("rule", user.rule.ToString());
                cmd.Parameters.AddWithValue("table_number", user.table_number);
                cmd.Parameters.AddWithValue("telegram_id", user.telegram_id);

                using NpgsqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Console.WriteLine($"Id = {reader[0].ToString()}");

                    try
                    {
                        user.id = Int32.Parse(reader[0].ToString());
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Ошибка, не найден id");
                    }
                }

            }


            return user;

        }



        public static List<StaffPerson> GetStaff(string addCondition = "")

        {
            string query = $"SELECT * FROM public.staff {addCondition} ORDER BY surname";

            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                var listForReturn = connect.Query<StaffPerson>(query);
                return listForReturn.ToList();
            }

        }

        public static StaffPerson? GetStaffPerson(string addCondition = "")

        {
            string query = $"SELECT * FROM public.staff Where {addCondition} ORDER BY surname";

            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                var personForReturn = connect.QueryFirstOrDefault<StaffPerson>(query);
                return personForReturn;
            }

        }

        public static List<StaffPerson> GetBosses()

        {
            string query = $"SELECT * FROM public.staff WHERE rule in ('admin','boss') and COALESCE(telegram_id,0)>0";



            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                var listForReturn = connect.Query<StaffPerson>(query);
                return listForReturn.ToList();
            }

        }

        public static void UpdateStaffTelegram_id(long? telegramId, int? tableNumber)
        {
            string query = $"UPDATE public.staff SET telegram_id={telegramId} WHERE table_number={tableNumber};";


            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                connect.Execute(query);

            }

        }

        public static void UpdateStaff(string nameColumn, string value, int? tableNumber)
        {
            string query = $"UPDATE public.staff SET {nameColumn}={value} WHERE table_number={tableNumber};";


            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                connect.Execute(query);

            }

        }

        public static void UpdateStaff(string nameColumn, string value, long? telegramId)
        {
            string query = $"UPDATE public.staff SET {nameColumn}={value} WHERE telegram_id={telegramId};";


            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                connect.Execute(query);

            }

        }

        public static void UpdateStaff(string nameColumn, int? value, long? telegramId)
        {
            string query = $"UPDATE public.staff SET {nameColumn}={value} WHERE telegram_id={telegramId};";


            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                connect.Execute(query);

            }

        }

        public static void UpdateStaffMultiple(string value, int? tableNumber)
        {
            string query = $"UPDATE public.staff SET {value} WHERE table_number={tableNumber};";


            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                connect.Execute(query);

            }

        }

        public static LastBotCommand? GetLastBotCommand(long? telegramId)
        {
            string query = $"SELECT command_type, date FROM public.last_bot_command WHERE telegram_id={telegramId};";


            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                connect.Open();

                using NpgsqlCommand cmd = new NpgsqlCommand(query, connect);


                using NpgsqlDataReader reader = cmd.ExecuteReader();

                LastBotCommand lastBotCommand = new() { Type = null, Date = DateTime.Now };

                while (reader.Read())
                {
                    Console.WriteLine($"Id = {reader[0].ToString()}");
                    var bot_command = reader[0].ToString();
                    var dateBotCommandStr = reader[1].ToString();

                    if (bot_command == null)
                    {
                        lastBotCommand = new() { Type = null, Date = DateTime.Now };

                    }
                    else
                    {
                        Bot_command_types last_command;
                        Enum.TryParse<Bot_command_types>(bot_command, out last_command);

                        DateTime dateBotCommand = DateTime.Now;

                        if (dateBotCommandStr != null)
                        {

                            dateBotCommand = DateTime.ParseExact(dateBotCommandStr, "dd.MM.yyyy H:mm:ss", CultureInfo.InvariantCulture);
                        }
                        lastBotCommand = new() { Type = last_command, Date = dateBotCommand };

                    }
                }


                return lastBotCommand;
            }

        }

        public static void UpdateLastBotCommand(long? telegram_id, Bot_command_types bot_command)
        {
            string query = $"INSERT INTO public.last_bot_command(telegram_id, command_type,date) VALUES ({telegram_id}, '{bot_command}',CURRENT_DATE) ON CONFLICT (telegram_id) DO UPDATE SET command_type = '{bot_command}',date=CURRENT_DATE;";

            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                connect.Execute(query);

            }

        }


        public static List<StaffPerson> GetStaffIfNotificationIsNecessary()

        {
            string query = $"SELECT * FROM public.staff WHERE COALESCE(reminder_is_necessary,false) and COALESCE(telegram_id,0)>0 and COALESCE(date_remind,Date '1900-01-01')<CURRENT_DATE and table_number not in (SELECT vacations.staff_table_number FROM public.vacations as vacations Where vacations.cancelled=false and vacations.date_end>=current_date and vacations.date_start<=current_date and vacation_type_id!=11 ORDER BY staff_table_number, date_start ASC)";



            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                var listForReturn = connect.Query<StaffPerson>(query);
                return listForReturn.ToList();
            }

        }

        public static List<Vacation> GetAllVacations()

        {
            string query = "SELECT staff.surname as Surname,staff.name as Name,vacations.staff_table_number, vacations.date_start,vacations.date_end,vacations.days  FROM public.vacations as vacations\r\ninner join public.staff as staff on vacations.staff_table_number=staff.table_number\r\nwhere vacations.planned=true and vacations.cancelled=false and vacations.date_start>date_trunc('year', now())\r\nORDER BY surname, date_start ASC ";


            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                var listForReturn = connect.Query<Vacation>(query);
                return listForReturn.ToList();
            }
        }

        public static List<string> GetAllStatusByPeriod(string period="week")
        {
            string query = "";
            if (period == "week")
            {
                query = "SELECT TO_CHAR(date, 'DD.MM.YYYY') as date, staff.surname, staff.name,vacations.staff_table_number, vacations.vacation_type_id, vacation_types.name, vacation_types.table_code FROM public.date_table left join public.vacations as vacations on date between date_start and date_end and not cancelled left join public.staff as staff on vacations.staff_table_number=staff.table_number left join public.vacation_types as vacation_types on vacations.vacation_type_id=vacation_types.id Where date between (current_date - Cast(extract(dow from current_date) as integer)) and current_date order by date, staff.surname;";
            }
            else
            {
                query = "SELECT TO_CHAR(date, 'DD.MM.YYYY') as date, staff.surname, staff.name,vacations.staff_table_number, vacations.vacation_type_id, vacation_types.name, vacation_types.table_code FROM public.date_table left join public.vacations as vacations on date between date_start and date_end and not cancelled left join public.staff as staff on vacations.staff_table_number=staff.table_number left join public.vacation_types as vacation_types on vacations.vacation_type_id=vacation_types.id Where date between (date_trunc('month', current_date)) and current_date order by date, staff.surname;";

            }
            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                connect.Open();

                using NpgsqlCommand cmd = new NpgsqlCommand(query, connect);


                using NpgsqlDataReader reader = cmd.ExecuteReader();

                List<string> reportOfStatus = new() { };

                string lastDate = "";

                while (reader.Read())
                {
                    Console.WriteLine($"Id = {reader[0].ToString()}");
                    var date = reader[0].ToString();
                    var surname = reader[1].ToString();
                    var name = reader[2].ToString();
                    var tableNumber = reader[3].ToString();
                    var vacationTypesName = reader[5].ToString();
                    var vacationTypesCode = reader[6].ToString();

                   
                    if (name!=null)
                    {
                        if (lastDate != date)
                        { 
                            lastDate = date;
                            reportOfStatus.Add($"{date}");
                        }

                        if (name == "") { reportOfStatus.Add(" - "); continue;}

                        reportOfStatus.Add($" {surname} {name} - {vacationTypesName} ({vacationTypesCode})");



                    }

                }


                return reportOfStatus;

            }
        }

        public static List<string> GetAllStatus()
        {
            
            string query = "SELECT staff.name, surname, patronymic, rule, table_number, telegram_id, reminder_is_necessary, date_remind, COALESCE(vacation_types.name,'В офисе'), COALESCE(vacation_types.table_code, 'Я') FROM public.staff as staff left join public.vacations as vacations on vacations.staff_table_number=staff.table_number and not vacations.cancelled and CURRENT_DATE between date_start and date_end left join public.vacation_types as vacation_types on vacations.vacation_type_id=vacation_types.id order by surname";

            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                connect.Open();

                using NpgsqlCommand cmd = new NpgsqlCommand(query, connect);


                using NpgsqlDataReader reader = cmd.ExecuteReader();

                List<string> reportOfStatus = new() { };

                while (reader.Read())
                {
                    Console.WriteLine($"Id = {reader[0].ToString()}");
                    var name = reader[0].ToString();
                    var surname = reader[1].ToString();
                    var tableNumber = reader[4].ToString();
                    bool reminderIsNecessary = Convert.ToBoolean(reader[6].ToString());
                    var dateRemind = reader[7].ToString();
                    var vacationTypesName = reader[8].ToString();
                    var vacationTypesCode = reader[9].ToString();

                    if (name != null)
                    {

                        reportOfStatus.Add($"{surname} {name} - {vacationTypesName} ({vacationTypesCode})");



                    }

                }


                return reportOfStatus;

            }
        }


        public static List<StaffPerson> UpdateAllStatusForUnnecessary()
        {
            string query = "UPDATE public.staff SET date_remind=current_date WHERE table_number in (SELECT table_number FROM public.staff WHERE COALESCE(reminder_is_necessary,false)=false and COALESCE(telegram_id,0)>0 and COALESCE(date_remind,Date '1900-01-01')<CURRENT_DATE and table_number not in (SELECT vacations.staff_table_number FROM public.vacations as vacations where vacations.cancelled=false and vacations.date_end>=current_date and vacations.date_start<=current_date ORDER BY staff_table_number, date_start ASC)) RETURNING table_number, surname,name;";

            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                var listForReturn = connect.Query<StaffPerson>(query);
                return listForReturn.ToList();

            }

        }

        public static string CreateNewRequest(Request newRequest)
        {
            int daysQuantity= newRequest.days_quantity;
            if (daysQuantity > 0)
            {
                daysQuantity--;
            }

            string dateStart = newRequest.date_start.GetValueOrDefault().ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            string vacationTypeName=VacationType.GetVacationTypeByID(newRequest.vacation_type_id);

            string query = $"INSERT INTO public.requests(staff_table_number, vacation_type_id, date_start, date_end, vacation_base_id, days_quantity, status, date) VALUES ({newRequest.staff_table_number}, {newRequest.vacation_type_id}, '{dateStart}'::date, '{dateStart}'::date+interval '{daysQuantity} day', null, {newRequest.days_quantity}, 'created', current_date) Returning date_end;";
            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                var dateEnd = connect.QueryFirstOrDefault<DateTime>(query);
                return $"{vacationTypeName}" +
                    $"\nДаты: с {dateStart} по {dateEnd.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)}";

            }

            
        }

        public static List<Request> GetRequestsForAgreement()
        {

            string query = "SELECT requests.id, staff_table_number, vacation_type_id, date_start, date_end, vacation_base_id, days_quantity, status, staff.surname as Surname, staff.name as Name, staff.telegram_id, CONCAT (vacation_types.name, ' (', vacation_types.table_code::text, ')')  as VacationTypeString FROM public.requests as requests left join public.staff as staff on staff_table_number=staff.table_number left join public.vacation_types as vacation_types on vacation_type_id=vacation_types.id Where status='created' order by id;";
            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                var listForReturn = connect.Query<Request>(query);
                return listForReturn.ToList();

            }
        }
    }

    
}
