using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace FanSync.Logging
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
        private const string dateFormat = "yyMMdd-HHmmss";
        private const int sizeLimit = 1 * 1024 * 1024;
        private const int maxFiles = 5;

        private string basePath;
        private string filename;

        private FileStream currentStream;

        public Logger(string basePath, string filename)
        {
            this.basePath = basePath;
            this.filename = filename;
        }

        private DateTime ParseDate(string text)
        {
            if (DateTime.TryParseExact(text, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            {
                return result;
            }
            else
            {
                return DateTime.MinValue;
            }
        }
        private List<string> ListLogFiles()
        {
            var filenames = new List<Tuple<DateTime, string>>();

            foreach (var path in Directory.GetFiles(basePath))
            {
                var filename = Path.GetFileName(path);
                if (filename.StartsWith(filename) && filename.EndsWith(".log"))
                {
                    var name = Path.GetFileNameWithoutExtension(filename);

                    string[] parts = name.Split(new char[] { '-' }, 2);
                    DateTime d = DateTime.MinValue;
                    if (parts.Length == 2)
                        d = ParseDate(parts[1]);

                    // TODO need to account for basePath
                    filenames.Add(Tuple.Create(d, path));
                }
            }

            return filenames.OrderBy(x => x.Item1).Select(x => x.Item2).ToList();
        }

        private string GenerateFilename()
        {
            var dateStr = DateTime.Now.ToString("yyMMdd-HHmmss");
            var newFilename = Path.Combine(basePath, $"{filename}-{dateStr}");
            return newFilename;
        }

        public void Log(LogLevel level, string message)
        {
            try
            {
                if (currentStream == null)
                {
                    var filenames = ListLogFiles();
                    var currentFilename = filenames.LastOrDefault() ?? GenerateFilename();
                    currentStream = new FileStream(currentFilename, FileMode.Append, FileAccess.Write);
                }

                if (currentStream.Position > sizeLimit)
                {
                    currentStream.Close();

                    currentStream = new FileStream(GenerateFilename(), FileMode.Append, FileAccess.Write);

                    var filenames = ListLogFiles();
                    for (int i = 0; i + maxFiles < filenames.Count; i++)
                    {
                        File.Delete(filenames[i]);
                    }
                }

                string date = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
                using (StreamWriter sw = new StreamWriter(currentStream, Encoding.UTF8))
                    sw.Write($"[{date}] | {level,-5} | {message}\r\n");

                currentStream.Flush();
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
