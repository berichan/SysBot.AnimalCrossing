using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossBot.SysBot
{
    public class ArrivalLog
    {
        private const string FileNameFormat = "ArrivalLog{0}.txt";
        private readonly string ArrivalLogDirectory = Path.Combine("logs", "arrivals");

        public ArrivalLog() 
        {
            if (!Directory.Exists(ArrivalLogDirectory))
                Directory.CreateDirectory(ArrivalLogDirectory);
        }

        public string UpdateLog(string newArrival, string dateTimeFormat = "HH:mm:ss tt")
        {
            var filename = GetFileNameToday();
            var entries = new List<string>();
            var now = DateTime.Now;
            if (!File.Exists(filename))
                File.Create(filename);
            else
                entries.AddRange(File.ReadAllLines(filename));

            var count = entries.FindAll(x => x.EndsWith($" {newArrival}"));
            var entry = $"[{now.ToString(dateTimeFormat)}] (Arrival count: {count.Count + 1}) {newArrival}";
            entries.Add(entry);
            File.WriteAllLines(filename, entries.ToArray());

            return entry;
        }

        public string GetFileNameToday()
        {
            var today = DateTime.Today;
            var fileName = string.Format(FileNameFormat, today.ToString("yyyyMMdd"));
            var path = Path.Combine(ArrivalLogDirectory, fileName);
            return path;
        }
    }
}
