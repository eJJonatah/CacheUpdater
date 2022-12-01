using CacheUpdater.Ambience;
using System.IO;

namespace CacheUpdater.App.ErrorHandler
{
    public class Handling
    {
        public struct log
        {
            public log(string message)
            {
                var logMessage = this.ToString() + ": " + message;
                if (!File.Exists(Context.Environment.ParamLocal + "logs.txt"))
                    File.Create(Context.Environment.ParamLocal + "logs.txt");
                File.AppendAllText(Context.Environment.ParamLocal + "logs.txt", "\n"
                    + logMessage);
                return;
            }
        }
    }
}