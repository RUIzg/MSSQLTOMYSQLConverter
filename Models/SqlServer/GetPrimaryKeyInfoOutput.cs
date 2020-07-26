using System;
using System.Collections.Generic;
using System.Text;

namespace MSSQLTOMYSQLConverter.Models.SqlServer
{
    public class GetPrimaryKeyInfoOutput
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public string CharacterMaximumLength { get; set; }
        public string IsNullable { get; set; }


    }
}
