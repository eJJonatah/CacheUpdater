using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using CacheUpdater.App.Models;
using CacheUpdater.App.Querys;
using System.Threading.Tasks;
using CacheUpdater.Ambience;
using Newtonsoft.Json;

namespace CacheUpdater.App
{
    public class Program
    {
        public static bool OperationSucess;
        public static bool RunFull = false;
        public static bool debug = false;
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            if (Context.Environment.Parameters.WaitDebug)
            {
                Console.WriteLine("Waiting for Debug \n To start Press AnyKey");
                Console.ReadKey(true);
            }
            if(debug != true)
            ArgHandler(args);
            Console.WriteLine($"Operation Starting. Running: {(Context.Environment.Parameters.EntireYear? "Entire Year" :$"{Context.Environment.Parameters.LastUpdate} to present")}");
            if(Context.Environment.Parameters.LastUpdate != DateTime.UtcNow.Date.ToString("yyyy-MM-dd") | Context.Environment.Parameters.EntireYear)
            {
                try
                {
                    Console.WriteLine("---Iniciando requisições---");
                    for (int i = 0; i < DbQuery.Querys.Length; i++)
                    {
                        var query = DbQuery.Querys[i];
                        if (Context.Environment.Parameters.EntireYear) { query.Sender = query.FullTime; }
                        else { query.Sender = query.UpdateNow; }
                        Console.Write("Montando Requisição...");
                        Console.CursorLeft = 0;
                        query.Sender.Parse();
                        string write = $"Solicitando tabela {query.Name}, aguardando retorno X";
                        Console.WriteLine(write);
                        int lastCursorLeft = write.ToCharArray().Length;
                        Console.CursorTop--;
                        var execution = new Task<string>(query.Sender.Run);
                        execution.Start();
                        int l = 0;
                        while (!execution.IsCompleted)
                        {
                            Console.CursorLeft = lastCursorLeft;
                            if (l >= LoadingChars.Length)
                                l = 0;
                            char loadChar = Convert.ToChar(LoadingChars[l]);
                            Console.CursorLeft--;
                            Console.WriteLine(loadChar);
                            Console.CursorTop--;
                            System.Threading.Thread.Sleep(130);
                            l++;
                        }
                        query.Sender.Result = execution.Result;
                        if (query.Sender.Result == null & query.Name == "Vendas")
                        {
                            Console.WriteLine("Consulta não retornou nenhum novo pedido                       ");
                        }
                        Console.CursorLeft = lastCursorLeft;
                        Console.CursorLeft--;
                        Console.WriteLine("OK");
                        query.Sender.Insert(CacheContext.Data.TablesJson, query.Name);
                        OperationSucess = !(query.Sender.Result == null);
                        Console.CursorTop--;
                        switch (OperationSucess)
                        {
                            case true:
                                Console.ForegroundColor = ConsoleColor.Green;
                                break;
                            case false:
                                Console.ForegroundColor = ConsoleColor.Red;
                                break;
                        }
                        Console.WriteLine($"Terminated query: {query.Name}, status: {(!OperationSucess? "Error" : "Sucess")}                      ");
                        Console.ForegroundColor = ConsoleColor.White;
                        System.Threading.Thread.Sleep(800);
                        if(!OperationSucess)
                            break;
                    }
                }
                catch (Exception e)
                {
                    var E = e;
                    while (E != null && E.InnerException != null)
                    {
                        var Except = E.InnerException;
                        Console.WriteLine(Except.Message);
                        E = Except.InnerException;
                    }
                    Environment.Exit(1);
                }
                if (Context.Environment.Parameters.EntireYear)
                {
                    MountingCache.Replace();
                }
                else
                {
                    MountingCache.Update();
                }
            }
            Console.WriteLine("\n Process Terminated. Saving Changes.");
            if (CacheContext.Data.TablesJson["Vendas"] == null)
                CacheContext.Data.TablesJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Context.Environment.CurrentDir + "\\Cache.json"));
            Console.WriteLine("Serializing CSV's");
            foreach (var table in CacheContext.Data.TablesJson)
            {
                DataTable JsonTable = (DataTable)JsonConvert.DeserializeObject<DataTable>(table.Value);
                if(JsonTable != null)
                {
                    Context.Environment.StoreFile(table.Key, Context.Environment.TabletoCSV(JsonTable));
                }
                Console.WriteLine($"Serialized {table.Key} to csv File");
            }
            if (Context.Environment.Parameters.WaitDebug)
            {
                Console.WriteLine("Process Ended. AnyKey to Exit.");
                Console.ReadKey();
            }
            Context.Environment.Update();
        }
        public static void ArgHandler(string[] args)
        {
                Console.WriteLine("Inputed Args:");
            if (args.Length != 0) 
            {
                foreach (var arg in args)
                {
                    Console.WriteLine('\n');
                    Console.Write('\t' + arg + ": ");
                    switch (true)
                    {
                        case true when arg == "-Full":
                            Console.Write("Running with entire year selection");
                            RunFull = true;
                            break;
                        default:
                            Console.Write("No Function Related");
                            break;
                    }
                }
            }
            else
            {
                Console.WriteLine('\t' + "No imputedArgs");
                Console.WriteLine('\n');
            }
        }
        public static string[] LoadingChars = new string[]
        {
            "/",
            "―",
            "\\",
            "|",
            "/",
            "―",
            "\\",
            "|"
        };
    }
}
