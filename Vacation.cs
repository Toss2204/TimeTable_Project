using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TimeTable_Project
{
    public class Vacation
    {
        public int id { get; set; }
        public int vacation_type_id { get; set; }
        public int staff_table_number { get; set; }
        public bool cancelled { get; set; }
        public bool planned { get; set; }
        public DateTime date_start { get; set; }
        public DateTime date_end { get; set; }       
        public int days {  get; set; }

        public string? Name { get; set; }
        public string? Surname { get; set; }

    }
}
