using System;
using System.Collections.Generic;
using MSSQLTOMYSQLConverter.Models;

namespace RokonoDbManager.Models
{
    public class OutboundTableDto
    {

        public OutboundTableDto(string tableName)
        {
            tableName = tableName?.Trim();
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException(nameof(tableName));
            }
            TableName = tableName;

        }

        /// <summary>
        /// 完整 转化后的 dll sql (mysql)
        /// </summary>
        public string TableDdlSql { get; set; }


        /// <summary>
        /// 列信息
        /// </summary>
        public List<BindingRowModel> RowInfo { get; set; }


        /// <summary>
        /// 表名
        /// </summary>
        public string TableName { get; private set; }


    }
}