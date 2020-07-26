using System;
using System.Collections.Generic;
using System.Text;
using RokonoDbManager.Models;

namespace MSSQLTOMYSQLConverter.Models
{
    public class GenerateSchemaOutput
    {

        /// <summary>
        /// 
        /// </summary>
        public string DbName { get; set; }


        public string Sql { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public string SaveToFilePath { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public List<OutboundTableDto> TableInfo { get; set; }


        public GenerateSchemaOutput()
        {
            TableInfo = new List<OutboundTableDto>();


        }
    }
}
