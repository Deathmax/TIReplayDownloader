using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TIReplayDownloader
{
    public class Demo
    {
        public string MatchID;
        public int MatchNumber;
        private string _description;
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                if (!string.IsNullOrEmpty(Series)) return;
                if (value.StartsWith("UB") || value.StartsWith("LB"))
                    Series = string.Join(" ", value.Split(' '), 0, 3);
                else
                    Series = string.Join(" ", value.Split(' '), 0, 1);

                if (value.StartsWith("LB") && !value.Contains("/"))
                    Game = "1/1";
                else
                    Game = value.Replace(Series + " ", "");
            }
        }
        public string TeamA;
        public string TeamB;
        public string Series;
        public string Game;
        public string Path;
        public int NumberOfGames;
    }
}
