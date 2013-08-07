using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ReplayPlayer
{
    class Program
    {
        /*private static string DotaPath =
            @"steam://run/570//-console -novid +demo_quitafterplayback 1 +demo_timescale 100 +playdemo ""replays\{0}""";
            //@"""F:\Program Files (x86)\Steam\steamapps\common\dota 2 beta\dota""";*/

        //private static string DotaParams =
        //    @"-novid +playdemo ""replays\{0}""";

        private static List<string> Demos = new List<string>(); 

        [STAThreadAttribute]
        static void Main(string[] args)
        {
            foreach (var arg in args)
            {
                if (Path.GetExtension(arg) != ".dem")
                {
                    AddFolder(arg);
                }
                else
                {
                    Demos.Add(GetReplayPath(arg));
                }
            }
            var commandstring = Demos.Aggregate("startdemos ", (current, demo) => current + ("\"replays\\" + demo + "\" "));
            Clipboard.SetText(commandstring);
            Console.WriteLine("Copied command to clipboard: {0}", commandstring);
            Console.WriteLine("Also wrote command to playdemos.cfg");
            File.WriteAllText("playdemos.cfg", commandstring);
            Console.Read();
        }

        static void AddFolder(string folder)
        {
            var dirfiles = Directory.GetFiles(folder);
            var tempdemos = dirfiles.Where(file => Path.GetExtension(file) == ".dem").Select(GetReplayPath).ToList();
            try
            {
                var tempdemos2 = tempdemos.ToDictionary(demo => int.Parse(Path.GetFileNameWithoutExtension(demo)));
                var sorted = tempdemos2.OrderBy(pair => pair.Key);
                foreach (var sort in sorted)
                {
                    Demos.Add(sort.Value);
                }
            }
            catch
            {
                Demos.AddRange(tempdemos);
            }
            foreach (var dir in Directory.GetDirectories(folder))
                AddFolder(dir);
        }

        static string GetReplayPath(string file)
        {
            var index = file.IndexOf("dota\\replays\\");
            return index != -1 ? file.Substring(index + "dota\\replays\\".Length) : file;
        }
    }
}
