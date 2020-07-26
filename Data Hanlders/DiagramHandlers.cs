using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using MSSQLTOMYSQLConverter;
using MSSQLTOMYSQLConverter.Data_Hanlders;
using MSSQLTOMYSQLConverter.DatabaseHandlers;
using rokono_cl.DatabaseHandlers;
using RokonoDbManager.Models;

namespace rokono_cl.Data_Hanlders
{
    public class DiagramHandlers
    {
        public static void GenerateSchema(SavedConnection parameter, string dbName, string dbFilePath)
        {
            var connStr = parameter.GetConnStr();

            using (var context = new DbManager(connStr))
            {

                var sqlServerMetaDataManager = new SqlServerMetaDataManager(connStr);

                var tables = sqlServerMetaDataManager.GetTables();
                var dbCreationScript = string.Empty;
                if(Program.DataBackup)
                    dbCreationScript += "SET GLOBAL FOREIGN_KEY_CHECKS=0;";
                var tablesForeignKeys = sqlServerMetaDataManager.GetTableForignKeys();
                
                foreach (var x in tables)
                {
                    if(Program.DataBackup)
                        dbCreationScript += context.GetTableRows(x); //todo zgr ,之后 再看是否需要改造

                    var od = context.GetTableData(x,tablesForeignKeys);
                    System.Console.WriteLine(od.CreationgString);
                    dbCreationScript += $"{od.CreationgString}\r\n";
                }

                var corelationData = context.GetDbUmlData();
                dbCreationScript += corelationData;
               //d System.Windows.Forms.Clipboard.SetText(dbCreationScript);
               System.Console.WriteLine("Clipboard middleware xclip for Linux is required otherwise the application throws an exception!");

                if(Program.DataBackup)
                    dbCreationScript += "SET GLOBAL FOREIGN_KEY_CHECKS=1;";


                WriteToFile(dbName , dbCreationScript); 
                SetClipboard(dbCreationScript);

            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected static void WriteToFile(string dbName ,  string dbCreationScript )
        {

            if (!File.Exists($"{dbName}.sql"))
            {
                using (var file = File.Create($"{dbName}.sql"))
                {

                }
            }
            File.WriteAllText($"{dbName}.sql", dbCreationScript);

            System.Console.WriteLine($"Creatiion script {dbName}.sql has been created in the root directory of the application in case the clipboard functionality fails!!! Known bug on Ubuntu based systems clipboard fails");

        }
        /// <summary>
        /// 
        /// </summary>
        protected static void SetClipboard( string dbCreationScript )
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                WindowsCopy.SetText(dbCreationScript);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                MacOsCopy.SetText(dbCreationScript);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                LinuxCopy.SetText(dbCreationScript);

            System.Console.WriteLine("Text has been saved to your clipboard, plase add it to your editor of choice. Don't froget to double check the generated data types if you like to change the defaults on your own!!!");
            var tempFileName = Path.GetTempFileName();
            Console.WriteLine($"{nameof(tempFileName)}:{tempFileName}");
        }
 

 
    }
}