using CacheUpdater.App.ErrorHandler;
using System.Collections.Generic;
using CacheUpdater.Connections;
using CacheUpdater.App.Models;
using CacheUpdater.Ambience;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq;
using System.IO;
using System;
using System.Windows.Media.Animation;
using System.Threading.Tasks;

namespace CacheUpdater.App.Querys
{
    public static class DbQuery
    {
        public static UpdatableQuery[] Querys;
        static DbQuery()
        {
            var queryData = JsonConvert.DeserializeObject<QueryInstruction>(File.ReadAllText(Context.Environment.CurrentDir + "\\Resc\\QueryStrings.json"));
            Querys = new UpdatableQuery[]
            {
                new UpdatableQuery("Vendas"),
                new UpdatableQuery("MixProdutos"),
                new UpdatableQuery("ClientesXVendedor"),
                new UpdatableQuery("PedidoXQuantidade"),
                new UpdatableQuery("PrecoPerData")
            };
            for(int i = 0; i < Querys.Length; i++)
            {
                var q = Querys[i];
                q = new UpdatableQuery(q.Name)
                {
                    FullTime = new QueryInfo(queryData.Saved.FullTime[q.Name]),
                    UpdateNow = new QueryInfo(queryData.Saved.UpdateNow[q.Name])
                };
                if (Context.Environment.Parameters.EntireYear)
                {
                    q.Storing = () => q.FullTime.Insert(CacheContext.Data.TablesJson, q.Name);
                }
                else
                {
                    q.Storing = () => q.UpdateNow.Insert(CacheContext.Data.TablesJson, q.Name);
                }
                Querys[i] = q;
            }
        }        
    }
    public struct UpdatableQuery
    {
        public string Name;
        public Action Storing;
        public QueryInfo FullTime;
        public QueryInfo UpdateNow;
        public QueryInfo Sender;
        public UpdatableQuery(string name) : this()
        {
            Name = name;
        }
        public void AddCache() => Storing.Invoke();
    }
    public struct QueryInfo
    {
        public string QueryString;
        public QueryInfo(string queryString) : this()
        {
            QueryString = queryString;
        }
        public string Result;
        public string Run()
        {
            var Query = new Connector.Odbc(Context.Environment.Parameters.DnsName)
            {
                QueryString = QueryString
            };
            Query.SendQuery().Wait();
            Result = Query.Json;
            return Result;
        }
        public void Insert( Dictionary<string,string> target, string name)
        {
            target[name] = Result;
        }
        public void Parse()
        {
            var parsingDetails = new Dictionary<string, object>();
            foreach(var item in Context.Environment.Parameters.ParseParams)
            {
                if(Context.Environment.RuntimeParams.TryGetValue(item.Value,out _)) 
                {
                    parsingDetails.Add(item.Key, Context.Environment.RuntimeParams[item.Value]) ;
                }
                else
                {
                    parsingDetails[item.Key] = item.Value;
                }
            }
            foreach(var spec in parsingDetails)
            {
                if(QueryString.Contains(spec.Key) && spec.Value is Func<string>)
                {
                    QueryString = QueryString.Replace(spec.Key, (spec.Value as Func<string>).Invoke());
                }
                else
                {
                    QueryString = QueryString.Replace(spec.Key, spec.Value.ToString());
                }

            }
        }
    }
    public static class MountingCache
    {
        public static void Truncate() => CacheContext.Data.TablesJson = null;
        public static void Replace()
        {
            try
            {
                File.WriteAllText(Context.Environment.Parameters.CachePath, JsonConvert.SerializeObject(CacheContext.Data.TablesJson));
            }
            catch (Exception e)
            {
                new Handling.log($"Can't find cache name file. One new will be created, raw {e.Message}");
                File.Create(Context.Environment.Parameters.CachePath);
                File.WriteAllText(Context.Environment.CurrentDir+"Cache.json",JsonConvert.SerializeObject(CacheContext.Data.TablesJson));

            }
        }
        public static void Update()
        {
            if (!File.Exists(Context.Environment.Parameters.CachePath))
            {
                Replace();
                return;
            }
            JObject info = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(Context.Environment.Parameters.CachePath));
            if (info == null) { Replace(); return; }
            JObject Caching = (JObject)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(CacheContext.Data.TablesJson));
            for (int i = 0; i < CacheContext.Data.TablesJson.Count; i++)
            {
                JArray pastObject = (JArray)JsonConvert.DeserializeObject((string)info[CacheContext.Data.TablesJson.ElementAt(i).Key]);
                JArray NewObject = (JArray)JsonConvert.DeserializeObject((string)CacheContext.Data.TablesJson.ElementAt(i).Value);
                pastObject.Merge(NewObject, new JsonMergeSettings
                {
                    MergeArrayHandling = MergeArrayHandling.Union,
                });
                string name = CacheContext.Data.TablesJson.ElementAt(i).Key;
                CacheContext.Data.TablesJson.Remove(name);
                CacheContext.Data.TablesJson.Add(name, JsonConvert.SerializeObject(pastObject));
            }
            File.WriteAllText(Context.Environment.Parameters.CachePath, JsonConvert.SerializeObject(CacheContext.Data.TablesJson));
        }
    }
    public class QueryInstruction
    {
        public QueryString Saved;
    }
    public class QueryString
    {
        public Dictionary<string, string> FullTime;
        public Dictionary<string, string> UpdateNow;
    }
}