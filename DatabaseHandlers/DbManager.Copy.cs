using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace rokono_cl.DatabaseHandlers
{
    public partial  class DbManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        internal string GetTableRows(string tableName)
        {
            var tableQuery = string.Empty;
            var query = $"SELECT * FROM {tableName}";
            SqlCommand command = new SqlCommand(query, _sqlConnection);

            // Open the connection in a try/catch block. 
            // Create and execute the DataReader, writing the result
            // set to the console window.
            try
            {

                _sqlConnection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    tableQuery += $"Insert into {tableName} values (";

                    for (var i = 0; i < reader.FieldCount - 1; i++)
                    {
                        var val = reader.GetValue(i);
                        if (i != reader.FieldCount - 1)
                            tableQuery += $"{GetValueByType(val)},";
                        else
                            tableQuery += $"{GetValueByType(val)}";
                    }

                    tableQuery += ");\r\n";
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                _sqlConnection.Close();

            }
            return tableQuery;
        }

        private object GetValueByType(object val)
        {
            if (val is string)
                val = $"'{val}'";
            if (val is DateTime)
                val = $"'{val}'";
            if (val == null)
                val = "null";
            return val;
        }



    }
}
