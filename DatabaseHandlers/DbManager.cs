

namespace rokono_cl.DatabaseHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using MSSQLTOMYSQLConverter.Models;
    using RokonoDbManager.Models;

    public class DbManager : IDisposable
    {
        SqlConnection SqlConnection; 
        public DbManager(string connectionString)
        {
            SqlConnection = new SqlConnection(connectionString);
        }

        internal List<string> GetTables()
        {
            var result = new List<string>();
            var query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'";
            var reader = ExecuteQuery(query);
            while (reader.Read())    
                result.Add(reader.GetString(0));
            reader.Close();
            SqlConnection.Close();
            return result;
        }

        public string GetDbUmlData()
        {
            var result = string.Empty;
            var query = "SELECT tp.name 'Parent table', cp.name 'Column Id',tr.name 'Refrenced table',cr.name 'Corelation Name' FROM  sys.foreign_keys fk INNER JOIN  sys.tables tp ON fk.parent_object_id = tp.object_id INNER JOIN  sys.tables tr ON fk.referenced_object_id = tr.object_id INNER JOIN  sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id INNER JOIN sys.columns cp ON fkc.parent_column_id = cp.column_id AND fkc.parent_object_id = cp.object_id INNER JOIN sys.columns cr ON fkc.referenced_column_id = cr.column_id AND fkc.referenced_object_id = cr.object_id ORDER BY tp.name, cp.column_id";
            SqlCommand command = new SqlCommand(query, SqlConnection);
            
            // Open the connection in a try/catch block. 
            // Create and execute the DataReader, writing the result
            // set to the console window.
            try
            {
                SqlConnection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                 
                        
                     result += $"ALTER TABLE {reader.GetString(0)} ADD FOREIGN KEY ({reader.GetString(1)}) REFERENCES {reader.GetString(2)}({reader.GetString(3)});\r\n";
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return result;
        }

        internal string GetTableRows(string x)
        {
            var tableQuery = string.Empty;
            var query = $"SELECT * FROM {x}";
            SqlCommand command = new SqlCommand(query, SqlConnection);
            
            // Open the connection in a try/catch block. 
            // Create and execute the DataReader, writing the result
            // set to the console window.
            try
            {

                SqlConnection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {   
                    tableQuery += $"Insert into {x} values (";
                    
                    for(var i= 0; i < reader.FieldCount -1; i++)
                    {
                        var val =  reader.GetValue(i);
                        if(i != reader.FieldCount - 1)
                            tableQuery += $"{GetValueByType(val)},";
                        else
                            tableQuery += $"{GetValueByType(val)}";
                    }
                    tableQuery += ");\r\n";
                }
                reader.Close();
                SqlConnection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return tableQuery;
        }

        private object GetValueByType(object val)
        {
            if(val is string)
                val = $"'{val}'";
            if(val is DateTime)
                val = $"'{val}'";
            if(val == null)
                val = "null";
            return val;
        }

        public List<OutboundTableConnection> GetTableForignKeys()
        {
            var result = new List<OutboundTableConnection> ();
            var query = "SELECT tp.name 'Parent table', cp.name 'Column Id',tr.name 'Refrenced table',cr.name 'Corelation Name' FROM  sys.foreign_keys fk INNER JOIN  sys.tables tp ON fk.parent_object_id = tp.object_id INNER JOIN  sys.tables tr ON fk.referenced_object_id = tr.object_id INNER JOIN  sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id INNER JOIN sys.columns cp ON fkc.parent_column_id = cp.column_id AND fkc.parent_object_id = cp.object_id INNER JOIN sys.columns cr ON fkc.referenced_column_id = cr.column_id AND fkc.referenced_object_id = cr.object_id ORDER BY tp.name, cp.column_id";
            SqlCommand command = new SqlCommand(query, SqlConnection);

            // Open the connection in a try/catch block. 
            // Create and execute the DataReader, writing the result
            // set to the console window.
            try
            {
                SqlConnection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new OutboundTableConnection{
                            TableName = reader.GetString(2),
                            ConnectionName = reader.GetString(3)

                    });
                        
                    //  result += $"ALTER TABLE {reader.GetString(0)} ADD FOREIGN KEY ({reader.GetString(1)}) REFERENCES {reader.GetString(2)}({reader.GetString(3)});";
                }
                reader.Close();
                SqlConnection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return result;
        }
        
        
        public OutboundTable GetTableData(string tableName, List<OutboundTableConnection> foreginKeys)
        {
            var result = new OutboundTable();
         
            var primaryAutoInc = string.Empty;
            var getPrimaryKey = ExecuteQuery($"SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = N'{tableName}'and COLUMNPROPERTY(object_id(TABLE_NAME), COLUMN_NAME, 'IsIdentity') = 1");
            while (getPrimaryKey.Read())    
            {
                primaryAutoInc = getPrimaryKey.GetString(0);
            }
            getPrimaryKey.Close();
            SqlConnection.Close();
            var query =$"SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = N'{tableName}'";
            var reader = ExecuteQuery(query);
           
            var notNull = "NOT NULL";
            var tableData = $"CREATE TABLE IF NOT EXISTS {tableName} (";
            var localData = new List<BindingRowModel>();
            var i = 0;
            while (reader.Read())    
            {
                
                if(reader.GetString(3) == "NO")
                    notNull = "NOT NULL";
                else
                    notNull = "";
                

                if(reader.GetString(0) == primaryAutoInc)
                    localData.Add(new BindingRowModel{
                        TableName = reader.GetString(0),
                        DataType = $"INT AUTO_INCREMENT PRIMARY KEY",
                        IsNull = notNull
                    });
                else if( foreginKeys.Any(x=>x.TableName == tableName && x.ConnectionName == reader.GetString(0)))
                    localData.Add(new BindingRowModel{
                        TableName = reader.GetString(0),
                        DataType = $"INT AUTO_INCREMENT PRIMARY KEY",
                        IsNull = notNull
                    });
                else if(reader.IsDBNull(2))
                    localData.Add(new BindingRowModel{
                        TableName = reader.GetString(0),
                        DataType = $"{DetermineType(reader.GetString(1), reader.IsDBNull(2) ? -1 : reader.GetInt32(2))}",
                        IsNull = notNull
                    });
             }
            var lastRow = localData.Count;
            localData.ForEach(x=>{
                i++;
                var next = ",";
                if(i == lastRow)
                    next = "";
                if(x.TableName == primaryAutoInc)
                    tableData += $"{x.TableName} {x.DataType}{next}";
                else
                    tableData += $"{x.TableName} {x.DataType} {x.IsNull}{next}";
            });
            tableData += " );";
            result.CreationgString = tableData;
            reader.Close();
            SqlConnection.Close();

            return result;
        }

        private string DetermineType(string v, int v1)
        {
            var res = string.Empty;
            var lenght = v1 != -1 ? $"({v1.ToString()})" : "";

            switch(v)
            {
                case "char":
                    if(v1 > 255)
                        res =  DetermineType("varchar",v1);
                    else
                        res = $"CHAR{lenght}"; 
                break;
                case "varchar":
                    if(v1 > 65535)
                        res =  DetermineType("text", v1);
                    else
                        res = $"VARCHAR{lenght}";
                break;
                case "text":
                    res = $"TEXT{lenght}";
                break;
                case "nchar":
                    res = $"VARCHAR{lenght}";
                break;
                case "nvarchar":
                    if(v1 > 65535 && v1 != -1)
                        DetermineType("text", v1);
                    else
                        res = $"VARCHAR{lenght}";
                break;  
                case "ntext":
                    res = $"LONGTEXT{lenght}";
                break;
                case "binary":
                    if(v1 > 65.535 && v1 != -1)
                        res = $"MEDIUMBLOB{lenght}";
                    else if(v1 >   16777215 && v1 != -1)
                        res = $"LONGBLOB{lenght}"; 
                    

                break;
                case "varbinary":
                       if(v1 > 65.535 && v1 != -1)
                        res = $"MEDIUMBLOB{lenght}";
                    else if(v1 >   16777215 && v1 != -1)
                        res = $"LONGBLOB{lenght}"; 
                break;
                case "varbinary(max)":
                       if(v1 > 65.535 && v1 != -1)
                        res = $"MEDIUMBLOB{lenght}";
                    else if(v1 >   16777215 && v1 != -1)
                        res = $"LONGBLOB{lenght}"; 
                break;
                case "image":
                       if(v1 > 65.535 && v1 != -1)
                        res = $"MEDIUMBLOB{lenght}";
                    else if(v1 > 16777215 && v1 != -1)
                        res = $"LONGBLOB{lenght}"; 
                break;
                case "bit":
                    res = $"CHAR"; 
                break;
                case "tinyint":
                    res = $"TINYINT{lenght}";
                break;
                case "smallint":
                    res = $"INT{lenght}";
                break;
                case "int":
                    res = $"INT{lenght}";
                break;
                case "bigint":
                    res = $"BIGINT{lenght}";
                break;
                case "decimal":
                    res =  $"DECIMAL{lenght}";
                break;
                case "numeric":
                    res = $"BIGINT{lenght}";

                break;
                case "smallmoney":
                    res = $"INT{lenght}";

                break;
                case "money":
                    res =  $"DECIMAL{lenght}";

                break;
                case "float":
                    res = $"FLOAT{lenght}";
                break;
                case "real":
                    res =  $"DECIMAL{lenght}";

                break;
                case "datetime":
                    res = $"DATETIME";
                break;
                case "datetime2":
                    res = $"DATETIME";
                break;
                case "smalldatetime":
                    res = $"DATETIME";

                break;
                case "date":
                    res = $"DATE";
                break;

                case "time":
                    res = "TIME";
                break;
                case "datetimeoffset":

                break;
                case "timestamp":
                    res = "TIMESTAMP";
                break;
             
            }
            return res;
        }

        public SqlDataReader ExecuteQuery(string query)
        {
            
            SqlCommand command = new SqlCommand(query, SqlConnection);
            try
            {
                SqlConnection.Open();
                return command.ExecuteReader();
              
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    SqlConnection.Close();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DbManager()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}