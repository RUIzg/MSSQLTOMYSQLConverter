namespace MSSQLTOMYSQLConverter.Models
{
    public class GetPrimaryKeyInfoOutput
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public string CharacterMaximumLength { get; set; }
        public string IsNullable { get; set; }




        /// <summary>
        /// 
        /// </summary>
        public int? NumericPrecision { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int? NumericScale { get; set; }
    }
}
