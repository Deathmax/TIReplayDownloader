using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TIReplayDownloader
{
    public class ConsoleExt
    {
        private static readonly List<string> _messages = new List<string>(51); 
        private static readonly List<ProgressBar> _progressBars = new List<ProgressBar>();
        private static int _barRows;

        public static void Log(string format, params object[] param)
        {
            Log(String.Format(format, param));
        }

        public static void Log(string message)
        {
            AddMessage(string.Format("{0} - {1}", DateTime.Now, message));
        }

        public static void Start()
        {
            new Thread(RenderLoop).Start();
        }

        public static void AddProgressBar(ProgressBar bar)
        {
            _progressBars.Add(bar);
        }

        private static void RenderLoop()
        {
            while (true)
            {
                RenderMessages();
                Thread.Sleep(500);
            }
        }

        private static void AddMessage(string message)
        {
            _messages.Add(message);
            //if (_messages.Count == 51)
            //    _messages = _messages.GetRange(1, 50).ToList();
        }

        private static void RenderMessages()
        {
            var newpos = Console.CursorTop - (_barRows);
            Console.CursorTop = newpos < 0 ? Console.CursorTop : newpos;
            _barRows = 0;
            var width = Console.WindowWidth - 1;
            if (_messages.Count > 0)
            {
                var writemessage = _messages.Aggregate("",
                                                       (current, message) =>
                                                       current +
                                                       message.PadRight((width + 1)*
                                                                        (int)
                                                                        Math.Ceiling((double) message.Length/width)));
                Console.Write(writemessage);
                _messages.Clear();
            }
            for (int i = 0; i < _progressBars.Count; i++)
            {
                var progressbar = _progressBars[i];
                if (progressbar.Destroy)
                {
                    if (progressbar.DestroyTicks == 5)
                    {
                        _progressBars.Remove(progressbar);
                        continue;
                    }
                    progressbar.DestroyTicks++;
                }
                if (i == 0)
                {
                    Console.Write(new string('-', width + 1));
                    _barRows++;
                }
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.ForegroundColor = ConsoleColor.Cyan;
                //Console.CursorLeft = 0;
                var barwidth = (int) (((width - 1)*progressbar.Progress)/100d);
                var barstring = new string('\u2592', barwidth) + new string(' ', width - barwidth - 1);
                Console.Write("\r[{0}]", barstring);
                _barRows++;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write((string.Join("", progressbar.Message.Take(width + 1))).PadRight(width + 1) + "\r");
                _barRows++;
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.BackgroundColor = ConsoleColor.Black;
            }
        }
    }

    public class ProgressBar
    {
        public string Message = "";
        public int Progress = 0;
        public bool Destroy = false;
        public int DestroyTicks = 0;
    }
}
