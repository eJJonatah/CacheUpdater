using CacheUpdater.App.ErrorHandler;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.Common;
using System.Data.Odbc;
using Newtonsoft.Json;
using System.Windows;
using System;

namespace CacheUpdater.Connections
{
    public static class Connector
    {
        public class Odbc
        {
            IEnumerable<Dictionary<string,object>> ResultDictionary;
            public string QueryString { get; set; }
            public string OdbcName;
            public string Json;
            object Response { get; set; }
            public Odbc(string odbcName)
            {
                OdbcName = odbcName;
            }
            public async Task SendQuery()
            {
                using (var DbConnection = new OdbcConnection("dsn=" + OdbcName))
                {
                    DbConnection.ConnectionTimeout = 3600;
                    DbDataReader result = null;
                    OdbcCommand Query = null;
                    try
                    {
                        DbConnection.Open();
                        Query = DbConnection.CreateCommand();
                        var timeoutQuery = DbConnection.CreateCommand();
                        timeoutQuery.CommandText = "set statement_timeout = '3600 s'";
                        timeoutQuery.ExecuteReaderAsync().Wait();
                        Query.CommandText = QueryString;
                        result = await Query.ExecuteReaderAsync();
                        Response = result;
                        ResultDictionary = Connector.Odbc.Serialize((DbDataReader)this.Response);
                        Json = JsonConvert.SerializeObject(ResultDictionary, Formatting.Indented);
                    }
                    catch (Exception e)
                    {
                        new Handling.log(e.Message);
                        throw e;
                    }
                    finally 
                    {
                        try
                        {
                            result.Close();
                        }
                        catch{}
                        try
                        {
                            Query.Dispose();
                        }
                        catch { }
                        DbConnection.Close();
                    }
                }
                return;
            }
            public static IEnumerable<Dictionary<string, object>> Serialize(DbDataReader reader)
            {
                var results = new List<Dictionary<string, object>>();
                var cols = new List<string>();
                for (var i = 0; i < reader.FieldCount; i++)
                    cols.Add(reader.GetName(i));
                while (reader.Read())
                    results.Add(SerializeRow(cols, reader));
                return results;
            }
            private static Dictionary<string, object> SerializeRow(IEnumerable<string> cols,
                                                            DbDataReader reader)
            {
                var result = new Dictionary<string, object>();
                foreach (var col in cols)
                    result.Add(col, reader[col]);
                return result;
            }
        }
    }
}