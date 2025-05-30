using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeTable_Project
{
    public class Request
    {
        public int id { get; set; }

        public DateTime? date { get; set; }
        public int? staff_table_number { get; set; }
        public int vacation_type_id { get; set; }
        public DateTime? date_start { get; set; }
        public DateTime? date_end { get; set; }
        public int vacation_base_id { get; set; }       
        public int days_quantity { get; set; }
        public StatusRequest status { get; set; }

        public string? Name { get; set; }
        public string? Surname { get; set; }

        public string? VacationTypeString { get; set; }

        public void UpdateRequest(string textMsg)
        {
            string textAnswer = textMsg.Replace("/", "");

            string query = $"UPDATE public.requests SET status='{textMsg}' WHERE id={id}; Select * From public.requests Where id={id}";

            using (var connect = new NpgsqlConnection(Config.SQLConnectionString))
            {
                var upsetRequest = connect.QueryFirstOrDefault<Request>(query);

                if (upsetRequest != null)
                { 
                      
                    if (textMsg == "done") 
                    {
                        var staffPerson = StaffList.GetStaffPerson($"table_number={upsetRequest.staff_table_number}");

                        if (staffPerson != null)
                        {
                            var newVacation=staffPerson.UpdateStatus(upsetRequest.vacation_type_id);

                            if (newVacation != null)
                            {
                                query = $"UPDATE public.requests SET vacation_base_id='{newVacation.id}' WHERE id={id}";
 
                                connect.Execute(query);
                                
                            }

                        }

                    }

                    
                
                }
            }


        }

    }
}
