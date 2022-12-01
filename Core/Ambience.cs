using System.Collections.Generic;
using CacheUpdater.App.Models;
using Newtonsoft.Json.Linq;
using CacheUpdater.App;
using Newtonsoft.Json;
using System.Text;
using System.Linq;
using System.Data;
using System.IO;
using System;

namespace CacheUpdater.Ambience
{
    public static class Context
    {
        public static class Environment
        {
            public static string CurrentDir = System.Environment.CurrentDirectory;
            public static string ParamLocal { get => CurrentDir + "\\Resc\\Parameters.json"; }
            public static Dictionary<string, object> RuntimeParams;
            public static ParametersInfo Parameters;
            public static void Import()
            {
                var paramData = File.ReadAllText(ParamLocal);
                if (string.IsNullOrEmpty(paramData))
                {
                    Parameters = new ParametersInfo();
                    Update();
                }
                Parameters = JsonConvert.DeserializeObject<ParametersInfo>(paramData);
                Parameters.CachePath = System.Environment.CurrentDirectory + "\\Cache.json";
            }
            static Environment()
            {
                if (Program.debug)
                    CurrentDir = "C:\\Users\\Usuario\\OneDrive - KAPEX\\Repository\\CacheUpdater\\";
                Import();
                if (Program.RunFull)
                    Parameters.EntireYear = true;
                if(Parameters.LastUpdate == null)
                {
                    Parameters.LastUpdate = DateTime.UtcNow.Date.ToString("yyyy-mm-dd").Substring(0, 4) + "-01-01";
                }
                RuntimeParams = new Dictionary<string, object>()
                {
                    {"<START_ANO>", DateTime.UtcNow.Date.ToString("yyyy-MM-dd").Substring(0,4) + "-01-01"},
                    {"<CURRENT_DATE>", DateTime.UtcNow.Date.ToString("yyyy-MM-dd")},
                    {"<LAST_UPDATE>", Parameters.LastUpdate },
                    {
                        "<USED_PROD>",new Func<string>(() =>
                        {
                            string result = null;
                            JArray tableJson = (JArray)JsonConvert.DeserializeObject(CacheContext.Data.TablesJson["Vendas"].ToString());
                            for (int i = 0; i < tableJson.Count; i++)
                            {
                                JObject row = (JObject)tableJson[i]; result += row["produtos"] + ", ";
                            }
                            List<string> duplicates = result.Replace(" ", "").Split(',').ToList();
                            result = String.Join(", ", duplicates.Distinct());
                            return result.Substring(0,result.Length - 2);
                        })
                    },
                    {
                        "<USED_ORDER>", new Func<string>(() =>
                        {
                            string result = null;
                            JArray tableJson = (JArray)JsonConvert.DeserializeObject((CacheContext.Data.TablesJson["Vendas"].ToString()));
                            foreach (JObject row in tableJson)
                            {
                                var obj = tableJson[0];
                                if(row == obj)
                                {
                                    result += "\'" + row["i_nrpedido"] + "\'";
                                }
                                else
                                {
                                    result += ", " + "\'" + row["i_nrpedido"] + "\'";
                                }
                            }
                            return result;
                        })
                    }
                };
            }
            public static void Update()
            {
                Parameters.EntireYear = false;
                Parameters.LastUpdate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
                var paramData = JsonConvert.SerializeObject(Parameters, Formatting.Indented);
                File.WriteAllText(ParamLocal, paramData);

            }
            public struct ParametersInfo
            {
                public string DnsName;
                public string MaxRangePriceDate;
                public string LastUpdate;
                public string CachePath;
                public bool   EntireYear;
                public bool   WaitDebug;
                public Dictionary<string, string> ParseParams;
            }
            public static string TabletoCSV(DataTable dt)
            {
                StringBuilder sb = new StringBuilder();
                string[] columnNames = dt.Columns.Cast<DataColumn>().
                                                  Select(column => column.ColumnName).
                                                  ToArray();

                sb.AppendLine(string.Join(";", columnNames));
                foreach (DataRow row in dt.Rows)
                {
                    string[] fields = row.ItemArray.Select(field => field.ToString()).
                                                    ToArray();
                    sb.AppendLine(string.Join(";", fields));
                }
                return sb.ToString();
            }
            public static void StoreFile(string name, string content)
            {
                string filePath = Environment.CurrentDir + $"\\Data\\{name}.txt";
                if (!File.Exists(filePath))
                    File.Create(filePath);
                    File.WriteAllText(filePath, content);
            }
            public static DataTable jsonStringToTable(JArray jsonContent)
            {
                string jsonString = JsonConvert.SerializeObject(jsonContent);
                return JsonConvert.DeserializeObject<DataTable>(jsonString);
            }
        }
    }
}