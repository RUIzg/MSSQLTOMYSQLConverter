namespace MSSQLTOMYSQLConverter.Models
{
    public class BindingRowModel
    {
        //public string TableName { get; set; }
        public string DataType { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public string IsNull
        {
            get
            {
                if (IsNullable == "NO")
                   return "NOT NULL";
                else
                    return "";

            }
        }

        public string ColumnName { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public string IsNullable { get; set; }


    }

}