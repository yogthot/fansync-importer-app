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
        private StreamWriter currentWriter;

        public Logger(string basePath, string filename)
        {
            this.basePath = basePath;
            this.filename = filename;
        }

        private DateTimeOffset ParseDate(string text)
        {
            if (DateTimeOffset.TryParseExact(text, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset result))
            {
                return result;
            }
            else
            {
                return DateTimeOffset.MinValue;
            }
        }
        private List<string> ListLogFiles()
        {
            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            var filenames = new List<Tuple<DateTimeOffset, string>>();

            foreach (var path in Directory.GetFiles(basePath))
            {
                var filename = Path.GetFileName(path);
                if (filename.StartsWith(filename) && filename.EndsWith(".log"))
                {
                    var name = Path.GetFileNameWithoutExtension(filename);

                    string[] parts = name.Split(new char[] { '-' }, 2);
                    DateTimeOffset d = DateTimeOffset.MinValue;
                    if (parts.Length == 2)
                        d = ParseDate(parts[1]);

                    filenames.Add(Tuple.Create(d, path));
                }
            }

            return filenames.OrderBy(x => x.Item1).Select(x => x.Item2).ToList();
        }

        private string GenerateFilename()
        {
            var dateStr = DateTimeOffset.Now.ToString("yyMMdd-HHmmss");
            var newFilename = Path.Combine(basePath, $"{filename}-{dateStr}.log");
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
                    currentWriter = new StreamWriter(currentStream);
                }

                if (currentStream.Position > sizeLimit)
                {
                    currentWriter.Close();

                    currentStream = new FileStream(GenerateFilename(), FileMode.Append, FileAccess.Write);
                    currentWriter = new StreamWriter(currentStream, Encoding.UTF8);

                    var filenames = ListLogFiles();
                    for (int i = 0; i + maxFiles < filenames.Count; i++)
                    {
                        File.Delete(filenames[i]);
                    }
                }

                string date = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                currentWriter.WriteLine($"[{date}] | {level,-5} | {message}");
                currentWriter.Flush();
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
