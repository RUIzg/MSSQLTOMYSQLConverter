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
        /// ���� ת����� dll sql (mysql)
        /// </summary>
        public string TableDdlSql { get; set; }


        /// <summary>
        /// ����Ϣ
        /// </summary>
        public List<BindingRowModel> RowInfo { get; set; }


        /// <summary>
        /// ����
        /// </summary>
        public string TableName { get; private set; }


    }
}