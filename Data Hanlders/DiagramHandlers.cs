using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using MSSQLTOMYSQLConverter.Data_Hanlders;
using rokono_cl.DatabaseHandlers;
using RokonoDbManager.Models;

namespace rokono_cl.Data_Hanlders
{
    public class DiagramHandlers
    {
        public static void GenerateSchema(SavedConnection parameter, string dbName, string dbFilePath)
        {
            
            var res = new List<UmlBindingData>();
            using(var context = new DbManager($"Server={parameter.Host};Database={dbName};User ID={parameter.Username};Password='{parameter.Password}';"))
            {
                var tableData = new UmlBindingData();

                var outboundData = new List<OutboundTable>();
                var tables = context.GetTables();
                var dbCreationScript = string.Empty;
                tables.ForEach(x=>{
                    var od = context.GetTableData(x);
                    outboundData.Add(od);
                    System.Console.WriteLine(od.CreationgString);
                    dbCreationScript += od.CreationgString;
                });
                tableData.Tables = outboundData;
                
                var corelationData = context.GetDbUmlData();
                dbCreationScript += corelationData;
               //d System.Windows.Forms.Clipboard.SetText(dbCreationScript);
                res.Add(tableData);
                if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    WindowsCopy.SetText(dbCreationScript);
                if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    MacOsCopy.SetText(dbCreationScript);
                if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    LinuxCopy.SetText(dbCreationScript);

                var tempFileName = Path.GetTempFileName();
                if(!File.Exists("LastDbCreationScript.txt"))
                {
                    using(var file = File.Create("LastDbCreationScript.txt"))
                    {
                        
                    }
                }
                File.WriteAllText("LastDbCreationScript.txt",dbCreationScript);

                System.Console.WriteLine("Text has been saved to your clipboard, plase add it to your editor of choice. Don't froget to double check the generated data types if you like to change the defaults on your own!!!");
            }
         }

         
 

 
    }
}