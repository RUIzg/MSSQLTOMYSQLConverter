using System;
using System.Collections.Generic;
using System.Text;

namespace MSSQLTOMYSQLConverter.Models
{
    public class GetRefColumnInfoOutput
    {
        public string ParentTable { get; set; }
        public string ColumnId { get; set; }
        public string RefrencedTable { get; set; }
        public string CorelationName { get; set; }



    }
}
