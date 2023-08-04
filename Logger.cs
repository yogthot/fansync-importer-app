using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FanSync
{
    public enum LogLevel
    {
        ERROR,
        WARN,
        INFO,
        DEBUG
    }

    public class Logger
    {
        private string filename;

        public Logger(string filename)
        {
            this.filename = filename;
        }

        public void Log(LogLevel level, string message)
        {
            try
            {
                string date = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
                File.AppendAllText(filename, $"[{date}] | {level,-5} | {message}\r\n");
            }
            catch { }
        }

        public void Error(string message)
        {
            Log(LogLevel.ERROR, message);
        }

        public void Info(string message)
        {
            Log(LogLevel.INFO, message);
        }
    }
}
