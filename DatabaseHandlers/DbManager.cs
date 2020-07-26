

using System.Text;
using Dapper;
using MSSQLTOMYSQLConverter.DatabaseHandlers;
using NLog;

namespace rokono_cl.DatabaseHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using MSSQLTOMYSQLConverter.Models;
    using RokonoDbManager.Models;

    public partial class DbManager : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly SqlConnection _sqlConnection;

        private readonly SqlServerMetaDataManager _sqlServerMetaDataManager;

        private static Logger _logger = null;
        public DbManager(string connectionString)
        {
            _sqlConnection = new SqlConnection(connectionString);
            _sqlServerMetaDataManager = new SqlServerMetaDataManager(connectionString);
            _logger = NLog.LogManager.GetLogger(typeof(DbManager).ToString());


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
        /// <param name="foreginKeys"></param>
        /// <returns></returns>
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
            var tableDataSb = new StringBuilder($"CREATE TABLE ");;
            //tableDataSb.Append(" IF NOT EXISTS ");
            tableDataSb.Append(" {tableName} ( ");


            tableDataSb.AppendLine();

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
                    try
                    {
                        localData.Add(new BindingRowModel
                        {
                            ColumnName = colItem.ColumnName,
                            DataType = $"{DetermineType(colItem.DataType, leng)}",
                            IsNull = notNull
                        });
                    }
                    catch (NotImplementedException e)
                    {
                        _logger.Error(e,$"sqlserver 数据类型转化mysql 失败");
                    }
             

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
                    tableDataSb .AppendLine($"{x.ColumnName} {x.DataType}{next}") ;
                else
                    tableDataSb.AppendLine($"{x.ColumnName} {x.DataType} {x.IsNull}{next}");
            });
            tableDataSb .AppendLine( " );");
            result.CreationgString = tableDataSb.ToString();
            return result;
        }

        /// <summary>
        /// 数据类型转化
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="dataLength"></param>
        /// <returns></returns>
        private string DetermineType(string dataType, int dataLength)
        {
            var res = string.Empty;
            var lenght = dataLength != -1 ? $"({dataLength.ToString()})" : "";

            switch(dataType)
            {
                case "char":
                    if(dataLength > 255)
                        res =  DetermineType("varchar",dataLength);
                    else
                        res = $"CHAR{lenght}"; 
                break;
                case "varchar":
                    if(dataLength > 65535)
                        res =  DetermineType("text", dataLength);
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
                    if(dataLength==-1)//nvarchar(max) convert to 	LONGTEXT
                    {
                        res = $" LONGTEXT ";

                    }
                    else if(dataLength > 65535 && dataLength != -1)
                        res =DetermineType("text", dataLength);
                    else
                        res = $"VARCHAR{lenght}";
                break;  
                case "ntext":
                    res = $"LONGTEXT{lenght}";
                break;
                case "binary":
                        res = $"LONGBLOB"; //参考  navicat 转化
                    //if(dataLength > 65.535 && dataLength != -1)
                    //    res = $"MEDIUMBLOB{lenght}";
                    //else if(dataLength >   16777215 && dataLength != -1)
                    //    res = $"LONGBLOB{lenght}";
                    //else if(dataLength>0)
                    //{
                    //    res = $"binary{lenght}";
                    //}
             

                    break;
                case "varbinary":
                       if(dataLength > 65.535 && dataLength != -1)
                        res = $"MEDIUMBLOB{lenght}";
                    else if(dataLength >   16777215 && dataLength != -1)
                        res = $"LONGBLOB{lenght}"; 
                       else 
                       {
                        res = $"LONGBLOB"; 
                       }
                break;
                case "varbinary(max)":
                       if(dataLength > 65.535 && dataLength != -1)
                        res = $"MEDIUMBLOB{lenght}";
                    else if(dataLength >   16777215 && dataLength != -1)
                        res = $"LONGBLOB{lenght}";
                break;
                case "image":
                       if(dataLength > 65.535 && dataLength != -1)
                        res = $"MEDIUMBLOB{lenght}";
                    else if(dataLength > 16777215 && dataLength != -1)
                        res = $"LONGBLOB{lenght}"; 
                break;
                case "bit":
                    res = $"tinyint";   //navicat 
                    //res = $"CHAR"; 
                break;
                case "tinyint":
                    res = $"TINYINT{lenght}";
                break;
                case "smallint":
                    res = $"smallint";
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
                    res = $" decimal(10,4) ";

                break;
                case "money":
                    res =  $"	DECIMAL(15,4)";

                break;
                case "float":
                    res = $"FLOAT{lenght}";
                break;
                case "real":
                    res =  $"real"; // http://www.sqlines.com/sql-server-to-mysql
                    //navicat 是转化 为 float

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
                    res = "datetime";
                break;
                case "timestamp": 
                    res = "TIMESTAMP";
                    //navicat--> longblob

                    break;

                case "xml":
                    res = "longtext";
                    break;



                case "uniqueidentifier":
                    res = "char(36)";
                    break;

                case "sql_variant":
                    res = "longblob";
                    break;



            }

            if (string.IsNullOrWhiteSpace(res))
            {
                throw new NotImplementedException($"{nameof(dataType)}:{dataType} , {nameof(dataLength)}:{dataLength}"); 
            }
            return res;
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