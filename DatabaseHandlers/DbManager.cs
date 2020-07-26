

using System.Text;
using Dapper;
using MSSQLTOMYSQLConverter.DatabaseHandlers;

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
        /// <summary>
        /// 
        /// </summary>
        private readonly SqlConnection _sqlConnection;

        private readonly SqlServerMetaDataManager _sqlServerMetaDataManager;

        public DbManager(string connectionString)
        {
            _sqlConnection = new SqlConnection(connectionString);
            _sqlServerMetaDataManager = new SqlServerMetaDataManager(connectionString);
        }


    
        /// <summary>
        /// 获取 外键引用
        /// </summary>
        /// <returns></returns>
        public string GetDbUmlData()
        {
            StringBuilder sb  = new StringBuilder(); //使用 stringbuilder 拼接更好
        
            // Open the connection in a try/catch block. 
            // Create and execute the DataReader, writing the result
            // set to the console window.
                var metaList = _sqlServerMetaDataManager.GetRefColumnInfo();
                metaList.ForEach(item =>
                {
                    sb.AppendLine(
                        $"ALTER TABLE {item.ParentTable} ADD FOREIGN KEY ({item.ColumnId}) REFERENCES {item.RefrencedTable}({item.CorelationName});");
                });

                var ret = sb.ToString();
            return ret;
        }

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
            if(val is string)
                val = $"'{val}'";
            if(val is DateTime)
                val = $"'{val}'";
            if(val == null)
                val = "null";
            return val;
        }


        public OutboundTable GetTableData(string tableName, List<OutboundTableConnection> foreginKeys)
        {
            var result = new OutboundTable();
         
            var primaryAutoInc = string.Empty;

            var primaryKeyInfos = _sqlServerMetaDataManager.GetPrimaryKeyInfo(tableName);
            if (primaryKeyInfos.Count() > 1)
            {
                //error 
                throw new NotImplementedException("还未实现 多主键mapper 功能 ");
            }

            primaryAutoInc = primaryKeyInfos.FirstOrDefault()?.ColumnName;
           
            var notNull = "NOT NULL";
            var tableData = $"CREATE TABLE IF NOT EXISTS {tableName} (";
            var localData = new List<BindingRowModel>();

            var columnList = _sqlServerMetaDataManager.GetColumnInfos(tableName);
       

            columnList.ForEach(colItem =>
            {
                if (colItem.IsNullable == "NO")
                    notNull = "NOT NULL";
                else
                    notNull = "";


                if (colItem.ColumnName == primaryAutoInc) //主键 ，自增处理
                {
                    localData.Add(new BindingRowModel
                    {
                        ColumnName = colItem.ColumnName,
                        DataType = $"INT AUTO_INCREMENT PRIMARY KEY",
                        IsNull = notNull
                    });
                }
                //外健处理
                else if (foreginKeys.Any(x => x.TableName == tableName && x.ConnectionName == colItem.ColumnName))
                {

                    localData.Add(new BindingRowModel
                    {
                        ColumnName = colItem.ColumnName,
                        DataType = $"INT AUTO_INCREMENT PRIMARY KEY",
                        IsNull = notNull
                    });
                }
                    
                else
                {
                    var leng = string.IsNullOrWhiteSpace(colItem.CharacterMaximumLength) ? -1 : Convert.ToInt32(colItem.CharacterMaximumLength);
                    localData.Add(new BindingRowModel
                    {
                        ColumnName = colItem.ColumnName,
                        DataType = $"{DetermineType(colItem.DataType,  leng)}",
                        IsNull = notNull
                });
                }
                   
            });




            var i = 0;
            var lastRow = localData.Count;
            localData.ForEach(x=>{
                i++;
                var next = ",";
                if(i == lastRow)
                    next = "";
                if(x.ColumnName == primaryAutoInc)
                    tableData += $"{x.ColumnName} {x.DataType}{next}";
                else
                    tableData += $"{x.ColumnName} {x.DataType} {x.IsNull}{next}";
            });
            tableData += " );";
            result.CreationgString = tableData;

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
            
            SqlCommand command = new SqlCommand(query, _sqlConnection);
            try
            {
                if (_sqlConnection.State != ConnectionState.Open) //try open 
                {
                    _sqlConnection.Open(); 
                }
                return command.ExecuteReader();
              
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
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
                    _sqlConnection.Close();
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