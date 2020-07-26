using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using MSSQLTOMYSQLConverter;
using MSSQLTOMYSQLConverter.Data_Hanlders;
using MSSQLTOMYSQLConverter.DatabaseHandlers;
using MSSQLTOMYSQLConverter.Models;
using NLog;
using rokono_cl.DatabaseHandlers;
using RokonoDbManager.Models;

namespace rokono_cl.Data_Hanlders
{
    public class DiagramHandlers
    {
        private static Logger   logger = NLog.LogManager.GetLogger(typeof(DiagramHandlers).ToString());

        public static GenerateSchemaOutput GenerateSchema(SavedConnection parameter, string dbName, string dbFilePath)
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
                logger.Info("Clipboard middleware xclip for Linux is required otherwise the application throws an exception!");
         

                if (Program.DataBackup)
                    dbCreationScript += "SET GLOBAL FOREIGN_KEY_CHECKS=1;";


                var sqlFilePath = WriteToFile(dbName , dbCreationScript); 
                SetClipboard(dbCreationScript);

                var ret = new GenerateSchemaOutput();
                ret.SaveToFilePath = sqlFilePath;
                ret.Sql = dbCreationScript;
                return ret;


            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected static string WriteToFile(string dbName ,  string dbCreationScript )
        {

            if (!File.Exists($"{dbName}.sql"))
            {
                using (var file = File.Create($"{dbName}.sql"))
                {

                }
            }
            var baseDir = Directory.GetCurrentDirectory();
            var filePath = Path.Combine(baseDir, $"{dbName}.sql");

            File.WriteAllText(filePath, dbCreationScript);

            logger.Info($"Creatiion script {dbName}.sql has been created in the root directory of the application in case the clipboard functionality fails!!! Known bug on Ubuntu based systems clipboard fails");

            logger.Debug($"sql file path:{filePath}");

            return filePath;

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

            logger.Info("Text has been saved to your clipboard, plase add it to your editor of choice. Don't froget to double check the generated data types if you like to change the defaults on your own!!!");
            var tempFileName = Path.GetTempFileName();
            Console.WriteLine($"{nameof(tempFileName)}:{tempFileName}");
        }
 

 
    }
}