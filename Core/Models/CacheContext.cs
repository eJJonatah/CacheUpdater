using System.Collections.Generic;

namespace CacheUpdater.App
{
    namespace Models
    {
        public static class CacheContext
        {
            public class Data
            {
                public static Dictionary<string, string> TablesJson = new Dictionary<string, string>()
                {
                    {"Vendas", null},
                    {"MixProdutos", null },
                    {"ClientesXVendedor", null},
                    {"PedidoXQuantidade", null},
                    {"PrecoPerData", null }
                };
            }
        }
    }
}