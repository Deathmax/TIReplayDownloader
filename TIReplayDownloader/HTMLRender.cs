using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TIReplayDownloader
{
    public class HTMLRender
    {
        private static readonly Regex _escapeCurlyLeft = new Regex(@"({)\n");
        private static readonly Regex _escapeCurlyRight = new Regex(@"(})\n");
        public static Dictionary<string, string> FormatStrings = new Dictionary<string, string>();  

        public static void Initialize()
        {
            FormatStrings.Add("UB Round 1A", "X vs X");
            FormatStrings.Add("UB Round 1A 1/3", "#");
            FormatStrings.Add("UB Round 1A 2/3", "#");
            FormatStrings.Add("UB Round 1A 3/3", "#");

            FormatStrings.Add("UB Round 1B", "X vs X");
            FormatStrings.Add("UB Round 1B 1/3", "#");
            FormatStrings.Add("UB Round 1B 2/3", "#");
            FormatStrings.Add("UB Round 1B 3/3", "#");

            FormatStrings.Add("UB Round 1C", "X vs X");
            FormatStrings.Add("UB Round 1C 1/3", "#");
            FormatStrings.Add("UB Round 1C 2/3", "#");
            FormatStrings.Add("UB Round 1C 3/3", "#");

            FormatStrings.Add("UB Round 1D", "X vs X");
            FormatStrings.Add("UB Round 1D 1/3", "#");
            FormatStrings.Add("UB Round 1D 2/3", "#");
            FormatStrings.Add("UB Round 1D 3/3", "#");

            FormatStrings.Add("UB Round 2A", "X vs X");
            FormatStrings.Add("UB Round 2A 1/3", "#");
            FormatStrings.Add("UB Round 2A 2/3", "#");
            FormatStrings.Add("UB Round 2A 3/3", "#");

            FormatStrings.Add("UB Round 2B", "X vs X");
            FormatStrings.Add("UB Round 2B 1/3", "#");
            FormatStrings.Add("UB Round 2B 2/3", "#");
            FormatStrings.Add("UB Round 2B 3/3", "#");

            FormatStrings.Add("UB Round 3A", "X vs X");
            FormatStrings.Add("UB Round 3A 1/3", "#");
            FormatStrings.Add("UB Round 3A 2/3", "#");
            FormatStrings.Add("UB Round 3A 3/3", "#");

            FormatStrings.Add("LB Round 1A", "X vs X");
            FormatStrings.Add("LB Round 1A 1/1", "#");

            FormatStrings.Add("LB Round 1B", "X vs X");
            FormatStrings.Add("LB Round 1B 1/1", "#");

            FormatStrings.Add("LB Round 1C", "X vs X");
            FormatStrings.Add("LB Round 1C 1/1", "#");

            FormatStrings.Add("LB Round 1D", "X vs X");
            FormatStrings.Add("LB Round 1D 1/1", "#");

            FormatStrings.Add("LB Round 2A", "X vs X");
            FormatStrings.Add("LB Round 2A 1/1", "#");

            FormatStrings.Add("LB Round 2B", "X vs X");
            FormatStrings.Add("LB Round 2B 1/1", "#");

            FormatStrings.Add("LB Round 2C", "X vs X");
            FormatStrings.Add("LB Round 2C 1/1", "#");

            FormatStrings.Add("LB Round 2D", "X vs X");
            FormatStrings.Add("LB Round 2D 1/1", "#");

            FormatStrings.Add("LB Round 3A", "X vs X");
            FormatStrings.Add("LB Round 3A 1/1", "#");

            FormatStrings.Add("LB Round 3B", "X vs X");
            FormatStrings.Add("LB Round 3B 1/1", "#");

            FormatStrings.Add("LB Round 4A", "X vs X");
            FormatStrings.Add("LB Round 4A 1/3", "#");
            FormatStrings.Add("LB Round 4A 2/3", "#");
            FormatStrings.Add("LB Round 4A 3/3", "#");

            FormatStrings.Add("LB Round 4B", "X vs X");
            FormatStrings.Add("LB Round 4B 1/3", "#");
            FormatStrings.Add("LB Round 4B 2/3", "#");
            FormatStrings.Add("LB Round 4B 3/3", "#");

            FormatStrings.Add("LB Round 5A", "X vs X");
            FormatStrings.Add("LB Round 5A 1/3", "#");
            FormatStrings.Add("LB Round 5A 2/3", "#");
            FormatStrings.Add("LB Round 5A 3/3", "#");

            FormatStrings.Add("LB Round 6A", "X vs X");
            FormatStrings.Add("LB Round 6A 1/3", "#");
            FormatStrings.Add("LB Round 6A 2/3", "#");
            FormatStrings.Add("LB Round 6A 3/3", "#");

            FormatStrings.Add("Grand Championship", "X vs X");
            FormatStrings.Add("Grand Championship 1/5", "#");
            FormatStrings.Add("Grand Championship 2/5", "#");
            FormatStrings.Add("Grand Championship 3/5", "#");
            FormatStrings.Add("Grand Championship 4/5", "#");
            FormatStrings.Add("Grand Championship 5/5", "#");

            Deserialize();

            Render();
        }

        private static void Deserialize()
        {
            if (!File.Exists("htmldict.txt")) return;
            var data = File.ReadAllLines("htmldict.txt");
            lock (FormatStrings)
            {
                foreach (var split in data.Select(line => line.Split('|')))
                {
                    FormatStrings[split[0]] = split[1];
                }
            }
        }

        private static void Serialize()
        {
            if (File.Exists("htmldict.txt")) File.Delete("htmldict.txt");
            lock (FormatStrings)
            {
                foreach (var pair in FormatStrings)
                {
                    File.AppendAllText("htmldict.txt", pair.Key + "|" + pair.Value + "\n");
                }
            }
        }

        public static void Render()
        {
            ConsoleExt.Log("Starting HTML render.");
            Serialize();
            var template = CleanTemplate(File.ReadAllText("indextemplate.html"));
            var array = new string[70];
            int i = 0;
            lock (FormatStrings)
            {
                foreach (var str in FormatStrings)
                {
                    array[i] = str.Value;
                    i++;
                }
            }
            template = string.Format(template, array);
            File.WriteAllText("index.html", template);
        }

        private static string CleanTemplate(string template)
        {
            template = _escapeCurlyLeft.Replace(template, "{{\n");
            template = _escapeCurlyRight.Replace(template, "}}\n");
            return template;
        }
    }
}
