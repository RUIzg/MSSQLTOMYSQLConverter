using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using MSSQLTOMYSQLConverter.Models;
using MSSQLTOMYSQLConverter.Models.SqlServer;
using RokonoDbManager.Models;
using SqlServerColumnInfoDto = MSSQLTOMYSQLConverter.Models.GetPrimaryKeyInfoOutput;

namespace MSSQLTOMYSQLConverter.DatabaseHandlers
{
    /// <summary>
    /// 
    /// </summary>
    public class SqlServerMetaDataManager
    {

        private readonly string _connectionString;


        public SqlServerMetaDataManager(string connectionString)
        {
            _connectionString = connectionString;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<string> GetTables()
        {
            var query = "SELECT TABLE_NAME as TableName FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'";

            using var connection = new SqlConnection(_connectionString);
            var retList = connection.Query<GetTablesOutput>(query).ToList();
            return retList.Select(x => x.TableName).ToList();
        }


        /// <summary>
        /// 获取表与表之间的引用关系 
        /// </summary>
        /// <returns></returns>
        public List<GetRefColumnInfoOutput> GetRefColumnInfo()
        {
            var query = @"
SELECT tp.name 'ParentTable', cp.name 'ColumnId',tr.name 'RefrencedTable',cr.name 'CorelationName'
FROM  sys.foreign_keys fk INNER JOIN  sys.tables tp ON fk.parent_object_id = tp.object_id 
INNER JOIN  sys.tables tr ON fk.referenced_object_id = tr.object_id
INNER JOIN  sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
INNER JOIN sys.columns cp ON fkc.parent_column_id = cp.column_id AND fkc.parent_object_id = cp.object_id
INNER JOIN sys.columns cr ON fkc.referenced_column_id = cr.column_id AND fkc.referenced_object_id = cr.object_id 
ORDER BY tp.name, cp.column_id
";


            using var connection = new SqlConnection(_connectionString);
            var createSqlList = connection.Query<GetRefColumnInfoOutput>(query).ToList();
            return createSqlList;
        }



        /// <summary>
        /// 获取主键信息
        /// </summary>
        /// <param name="tableName"></param>
        public List<SqlServerColumnInfoDto> GetPrimaryKeyInfo(string tableName)
        {
            tableName = tableName?.Trim();
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));

            var query = @$"SELECT COLUMN_NAME as ColumnName, DATA_TYPE DataType,
CHARACTER_MAXIMUM_LENGTH CharacterMaximumLength, IS_NULLABLE as IsNullable
                         FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = N'{tableName}' 
                        and COLUMNPROPERTY(object_id(TABLE_NAME),
COLUMN_NAME, 'IsIdentity') = 1 ";

            using var connection = new SqlConnection(_connectionString);
            var createSqlList = connection.Query<SqlServerColumnInfoDto>(query).ToList();
            return createSqlList;


        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<SqlServerColumnInfoDto> GetColumnInfos( string tableName )
        {
            tableName = tableName?.Trim();
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));


            var sql =
                @$"SELECT COLUMN_NAME as ColumnName, DATA_TYPE DataType , CHARACTER_MAXIMUM_LENGTH CharacterMaximumLength , IS_NULLABLE IsNullable
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = N'{tableName}'  ";

            using var connection = new SqlConnection(_connectionString);
            var createSqlList = connection.Query<SqlServerColumnInfoDto>(sql).ToList();
            return createSqlList;


        }



        /// <summary>
        /// 至于为什么这样 转， 我也不知道 
        /// </summary>
        /// <returns></returns>
        public List<OutboundTableConnection> GetTableForignKeys()
        {
            var result = new List<OutboundTableConnection>();
#pragma warning disable CS0219 // 变量“query”已被赋值，但从未使用过它的值
            var query = @"SELECT tp.name 'Parent table', cp.name 'Column Id',tr.name 'Refrenced table',cr.name 'Corelation Name'
FROM  sys.foreign_keys fk INNER JOIN  sys.tables tp ON fk.parent_object_id = tp.object_id
INNER JOIN  sys.tables tr ON fk.referenced_object_id = tr.object_id
INNER JOIN  sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id 
INNER JOIN sys.columns cp ON fkc.parent_column_id = cp.column_id AND fkc.parent_object_id = cp.object_id
INNER JOIN sys.columns cr ON fkc.referenced_column_id = cr.column_id AND fkc.referenced_object_id = cr.object_id 
ORDER BY tp.name, cp.column_id";
#pragma warning restore CS0219 // 变量“query”已被赋值，但从未使用过它的值


            var tableRefInfo  = GetRefColumnInfo();
            var ret= tableRefInfo.Select(x=> new OutboundTableConnection
            {
                TableName = x.RefrencedTable,
                ConnectionName =x.CorelationName
            }).ToList();

            return ret;
        }


    }
}
