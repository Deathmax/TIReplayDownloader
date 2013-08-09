using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Zip;
using MegaLibrary;
using MegaApi;
using Mega2 = MegaApi.Mega;
using Mega = MegaLibrary.Mega;

namespace TIReplayDownloader
{
    class Program
    {
        public static string SaveDirectory;
        public static Dictionary<int, Demo> DemoList = new Dictionary<int, Demo>();
        public static List<int> MatchList = new List<int>();
        public static Dictionary<string, List<string>> MatchIDList = new Dictionary<string, List<string>>(); 
        public static Regex DescriptionRegex = new Regex(@"<div class=""resultsPageBoxHeader"">(.+)</div>");
        public static Regex TeamARegex = new Regex(@"<div class=""rpbTeamAName"">(.+)</div>");
        public static Regex TeamBRegex = new Regex(@"<div class=""rpbTeamBName"">(.+)</div>");
        public static Regex MatchIDRegex = new Regex(@"matchid=([0-9]+)");
        public static Regex DemoURLRegex = new Regex(@"<a href=""(.+\.bz2)"" class=""btn btn-primary"">");
        public static Regex DemoNameRegex = new Regex(@"([0-9]+_[0-9]+.dem)");
        public static Regex MatchNumberRegex = new Regex(@"<a href=""http://www.dota2.com/international/game/([0-9]+)"">View</a>");
        public static string ScheduleURL = "http://www.dota2.com/international/prelims/schedule/monday/";
        public static bool CompressSeries = true;
        public static Mega Mega;
        public static Mega2 Mega2;

        public static int Downloads;

        static void Main(string[] args)
        {
            SaveDirectory = args[0];
            ScheduleURL = args[1];
            CompressSeries = args[2] == "compressseries";
            ConsoleExt.Start();
            HTMLRender.Initialize();
            var downloaded = File.ReadAllLines("downloaded.txt");
            Mega = new Mega();
            foreach (var line in downloaded)
            {
                MatchList.Add(int.Parse(line));
                DemoList.Add(int.Parse(line), null);
            }
            ConsoleExt.Log("Starting.");
            if (CompressSeries)
            {
                var account = File.ReadAllLines("megaaccount.txt");
                ConsoleExt.Log("Logging into Mega.");
                Mega2.Init(new MegaUser(account[0], account[1]), (a =>
                                                                      {
                                                                          ConsoleExt.Log("Logged into Mega.");
                                                                          Mega2 = a;
                                                                          Mega.sid2 = a.User.Sid;
                                                                      }), (a =>
                                                                               {
                                                                                   ConsoleExt.Log(
                                                                                       "Failed to log into Mega. Error code: {0}",
                                                                                       a);
                                                                                   Mega2 = null;
                                                                               }));
            }
            if (!File.Exists("matchiddownload.txt"))
            {
                ConsoleExt.Log("Starting spider on {0}.", ScheduleURL);
                CheckLoop();
            }
            else
            {
                ConsoleExt.Log("Found matchiddownload.txt, forcing regular downloads.");
                var data = File.ReadAllLines("matchiddownload.txt");
                var first = true;
                var series = "";
                foreach (string t in data)
                {
                    if (first)
                    {
                        series = t;
                        first = false;
                    }
                    else if (string.IsNullOrWhiteSpace(t))
                    {
                        first = true;
                    }
                    else
                    {
                        if (!MatchIDList.ContainsKey(series))
                        {
                            MatchIDList.Add(series, new List<string>());
                        }
                        MatchIDList[series].Add(t);
                    }
                }
                foreach (var pair in MatchIDList)
                {
                    for (int i = 0; i < pair.Value.Count; i++)
                    {
                        var game = pair.Value[i];
                        DownloadDemoId(game, pair.Key, ((i + 1) == pair.Value.Count));
                    }
                }
            }
            ConsoleExt.Log("Done.");
            Console.Read();
        }

        static void CheckLoop()
        {
            while (true)
            {
                using (var wc = new WebClient {Proxy = null})
                {
                    try
                    {
                        if (File.Exists("uploadlist.txt"))
                        {
                            foreach (var line in File.ReadAllLines("uploadlist.txt"))
                            {
                                Compress(line);
                            }
                            File.Delete("uploadlist.txt");
                        }
                        var response = wc.DownloadString(ScheduleURL);
                        var matches = MatchNumberRegex.Matches(response);
                        ConsoleExt.Log("Schedule found {0} matches.", matches.Count);
                        foreach (var matchnum in from Match match in matches select int.Parse(match.Groups[1].Captures[0].Value))
                        {
                            if (!MatchList.Contains(matchnum))
                            {
                                ConsoleExt.Log("Found new game: {0}", matchnum);
                                MatchList.Add(matchnum);
                            }
                            if (DemoList.ContainsKey(matchnum)) continue;
                            while (Downloads >= 6) Thread.Sleep(100);
                            var demo = DownloadDemoWeb(matchnum);
                            if (demo != null)
                            {
                                DemoList.Add(matchnum, demo);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleExt.Log("Error occured getting schedule. Exception: {0}", ex.Message);
                    }
                }
                Thread.Sleep(5000);
            }
        }

        static Demo DownloadDemoWeb(int matchnum)
        {
            ConsoleExt.Log("Starting download of {0}", matchnum);
            using (var wc = new WebClient {Proxy = null})
            {
                try
                {
                    var response = wc.DownloadString("http://www.dota2.com/international/game/" + matchnum);
                    if (response.Contains("This game was not played."))
                    {
                        if (ScheduleURL.Contains("mainevent"))
                        {
                            var dem = new Demo
                                          {Description = DescriptionRegex.Match(response).Groups[1].Captures[0].Value};
                            if (dem.Description.Contains("3/3") || dem.Description.Contains("4/5") || dem.Description.Contains("5/5"))
                            {
                                //File.AppendAllText("uploadlist.txt", dem.Series + "\r\n");
                                if (!File.Exists(Path.Combine(SaveDirectory, "TI3 - " + dem.Series + ".zip")))
                                {
                                    File.AppendAllText("downloaded.txt", matchnum + "\r\n");
                                    ConsoleExt.Log("{0} was not played, compressing series.", matchnum);
                                    Compress(dem.Series);
                                }
                                return new Demo();
                            }
                            ConsoleExt.Log("{0} is not uploaded yet.", matchnum);
                            return null;
                        }
                        ConsoleExt.Log("{0} is not uploaded yet.", matchnum);
                        return null;
                    }
                    var demo = new Demo
                                   {
                                       Description = DescriptionRegex.Match(response).Groups[1].Captures[0].Value,
                                       TeamA = TeamARegex.Match(response).Groups[1].Captures[0].Value,
                                       TeamB = TeamBRegex.Match(response).Groups[1].Captures[0].Value,
                                       MatchID = MatchIDRegex.Match(response).Groups[1].Captures[0].Value,
                                       MatchNumber = matchnum
                                   };
                    if (demo.Series.StartsWith("Series"))
                        demo.NumberOfGames = 4;
                    else if (demo.Series.StartsWith("UB"))
                        demo.NumberOfGames = 3;
                    else if (demo.Series.StartsWith("LB"))
                        demo.NumberOfGames = demo.Game.Contains("/") ? 3 : 1;
                    else if (demo.Series.StartsWith("Grand"))
                        demo.NumberOfGames = 5;
                    var demoresponse = wc.DownloadString("https://rjackson.me/tools/matchurls?matchid=" + demo.MatchID);
                    var demourl = DemoURLRegex.Match(demoresponse).Groups[1].Captures[0].Value;
                    var demoname = DemoNameRegex.Match(demoresponse).Groups[1].Captures[0].Value.Split('_')[0] + ".dem.bz2";
                    if (File.Exists(Path.Combine(SaveDirectory, "TI3 - " + demo.Series,
                                                                                Path.GetFileNameWithoutExtension(demoname))))
                    {
                        File.AppendAllText("downloaded.txt", matchnum + "\r\n");
                        File.AppendAllText(Path.Combine(SaveDirectory, "TI3 - " + demo.Series, "details.txt"),
                                           string.Format(
                                               "{0} - {1} - {2} - {3} vs {4} | {5}\r\n{6}\r\n\r\n",
                                               demo.MatchNumber, demo.MatchID, demo.Description,
                                               demo.TeamA, demo.TeamB, "dota2://matchid=" + demo.MatchID,
                                               "playdemo \"" + @"replays\" + "TI3 - " +
                                               demo.Series + @"\" + Path.GetFileNameWithoutExtension(demoname) + "\""));
                        AddHTML(demo);
                        HTMLRender.Render();
                        return demo;
                    }
                    ConsoleExt.Log("Downloading {0}.", demourl);
                    var progressbar = new ProgressBar() {Message = "Downloading " + demourl};
                    wc.DownloadProgressChanged += (sender, args) =>
                                                      {
                                                          progressbar.Progress = args.ProgressPercentage;
                                                          var padded = demo.Description.PadRight(Console.WindowWidth);
                                                          var size = string.Format("{0:#,0}/{1:#,0}KB, {2}%",
                                                                                   args.BytesReceived/1000,
                                                                                   args.TotalBytesToReceive/1000,
                                                                                   args.ProgressPercentage);
                                                          progressbar.Message =
                                                              padded.Insert(padded.Length - size.Length,
                                                                            size);
                                                      };
                    wc.DownloadFileCompleted += (sender, args) =>
                                                    {
                                                        Downloads--;
                                                        if (args.Cancelled || args.Error != null)
                                                        {
                                                            progressbar.Destroy = true;
                                                            if (args.Error.Message.Contains("404"))
                                                            {
                                                                progressbar.Message = string.Format("{0}'s demo file is pending upload.", matchnum);
                                                            }
                                                            else
                                                                progressbar.Message = string.Format(
                                                                    "Downloading of {0} failed. Reason: {1}", matchnum,
                                                                    args.Cancelled ? "Cancelled" : args.Error.ToString());
                                                            DemoList.Remove(matchnum);
                                                            return;
                                                        }
                                                        try
                                                        {
                                                            Decompress(Path.Combine(SaveDirectory, "TI3 - " + demo.Series,
                                                                                demoname), progressbar);
                                                            File.AppendAllText("downloaded.txt", matchnum + "\r\n");
                                                            var path = Path.Combine(SaveDirectory, "TI3 - " + demo.Series, "details.txt");
                                                            File.AppendAllText(path,
                                                                               string.Format(
                                                                                   "{0} - {1} - {2} - {3} vs {4} | {5}\r\n{6}\r\n\r\n",
                                                                                   demo.MatchNumber, demo.MatchID, demo.Description,
                                                                                   demo.TeamA, demo.TeamB, "dota2://matchid=" + demo.MatchID,
                                                                                   "playdemo \"" + @"replays\" + "TI3 - " +
                                                                                   demo.Series + @"\" + Path.GetFileNameWithoutExtension(demoname) + "\""));
                                                            AddHTML(demo);
                                                            HTMLRender.Render();
                                                            if (Directory.GetFiles(Path.Combine(SaveDirectory, "TI3 - " + demo.Series)).Count(s => Path.GetExtension(s) == ".dem") >= demo.NumberOfGames
                                                                && !File.Exists(Path.Combine(SaveDirectory, "TI3 - " + demo.Series + ".zip")))
                                                            {
                                                                ConsoleExt.Log(
                                                                    "Starting compression and upload of {0}.", matchnum);
                                                                Compress(demo.Series);
                                                            }
                                                            else
                                                            {
                                                                progressbar.Destroy = true;
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            progressbar.Destroy = true;
                                                            ConsoleExt.Log("Exception occured finalizing file: {0}", ex.Message);
                                                        }
                                                    };
                    Directory.CreateDirectory(Path.Combine(SaveDirectory, "TI3 - " + demo.Series));
                    Downloads++;
                    wc.DownloadFileAsync(new Uri(demourl), Path.Combine(SaveDirectory, "TI3 - " + demo.Series, demoname));
                    ConsoleExt.AddProgressBar(progressbar);
                    demo.Path = Path.Combine(SaveDirectory, "TI3 - " + demo.Series, demoname);
                    return demo;
                }
                catch (Exception ex)
                {
                    ConsoleExt.Log("Download of {0} failed. Exception: {1}", matchnum, ex);
                    return null;
                }
            }
        }

        static Demo DownloadDemoId(string matchdata, string series, bool compress = false)
        {
            using (var wc = new WebClient { Proxy = null })
            {
                var matchid = matchdata.Split(new[] {' '}, 2)[0];
                var game = matchdata.Split(new[] {' '}, 2)[1];
                ConsoleExt.Log("Starting download of {0} - {1}.", matchid, game);
                var demo = new Demo
                               {
                                   Description = series + " " + game,
                                   Game = game,
                                   MatchID = matchid,
                                   MatchNumber = -1,
                                   Series = series
                               };
                var demoresponse = wc.DownloadString("https://rjackson.me/tools/matchurls?matchid=" + demo.MatchID);
                var demourl = DemoURLRegex.Match(demoresponse).Groups[1].Captures[0].Value;
                var demoname = DemoNameRegex.Match(demoresponse).Groups[1].Captures[0].Value.Split('_')[0] + ".dem.bz2";
                ConsoleExt.Log("Downloading {0}.", demourl);
                var progressbar = new ProgressBar();
                wc.DownloadProgressChanged += (sender, args) =>
                                                  {
                                                      progressbar.Progress = args.ProgressPercentage;
                                                      var padded = demo.Description.PadRight(Console.WindowWidth);
                                                      var size = string.Format("{0:#,0}/{1:#,0}KB, {2}%",
                                                                               args.BytesReceived/1000,
                                                                               args.TotalBytesToReceive/1000,
                                                                               args.ProgressPercentage);
                                                      progressbar.Message = padded.Insert(padded.Length - size.Length,
                                                                                          size);
                                                  };
                wc.DownloadFileCompleted += (sender, args) =>
                                                {
                                                    Downloads--;
                                                    if (args.Cancelled || args.Error != null)
                                                    {
                                                        if (args.Error.Message.Contains("404"))
                                                            ConsoleExt.Log("{0}'s demo file is pending upload.");
                                                        else
                                                            ConsoleExt.Log("Downloading of {0} failed. Reason: {1}",
                                                                           demo.MatchID,
                                                                           args.Cancelled
                                                                               ? "Cancelled"
                                                                               : args.Error.Message);
                                                        return;
                                                    }
                                                    try
                                                    {
                                                        Decompress(Path.Combine(SaveDirectory, demo.Series,
                                                                                demoname), progressbar);
                                                        var path = Path.Combine(SaveDirectory, demo.Series,
                                                                                "details.txt");
                                                        File.AppendAllText(path,
                                                                           string.Format(
                                                                               "{0} - {1} - {2} | {3}\r\n{4}\r\n\r\n",
                                                                               demo.MatchID, demo.Series,
                                                                               demo.Game,
                                                                               "dota2://matchid=" + demo.MatchID,
                                                                               "playdemo \"" + @"replays\" + 
                                                                               demo.Series + @"\" +
                                                                               Path.GetFileNameWithoutExtension(demoname) +
                                                                               "\""));
                                                        if (compress)
                                                            Compress(demo.Series);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        ConsoleExt.Log("Exception occured finalizing file: {0}", ex.Message);
                                                    }
                                                };
                Directory.CreateDirectory(Path.Combine(SaveDirectory, demo.Series));
                Downloads++;
                ConsoleExt.AddProgressBar(progressbar);
                wc.DownloadFileAsync(new Uri(demourl), Path.Combine(SaveDirectory, demo.Series, demoname));
                demo.Path = Path.Combine(SaveDirectory, demo.Series, demoname);
                return demo;
            }
        }

        static void Decompress(string srcfile, ProgressBar bar = null)
        {
            if (bar == null) ConsoleExt.Log("Decompressing {0}.", Path.GetFileName(srcfile));
            bar.Message = string.Format("Decompressing {0}.", Path.GetFileName(srcfile));
            using (var stream = new BZip2InputStream(new FileStream(srcfile, FileMode.Open)))
            {
                using (var file = File.Create(Path.Combine(Path.GetDirectoryName(srcfile), Path.GetFileNameWithoutExtension(srcfile))))
                {
                    var buffer = new byte[2048];
                    int n;
                    while ((n = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        file.Write(buffer, 0, n);
                    }
                }
            }
            if (bar == null)
            {
                ConsoleExt.Log("Decompressed {0}.", Path.GetFileName(srcfile)); 
                ConsoleExt.Log("Deleting {0}", Path.GetFileName(srcfile));
            }
            bar.Message = string.Format("Decompressed {0}, deleting.", Path.GetFileName(srcfile));
            bar.Destroy = true;
            File.Delete(srcfile);
        }

        static void Compress(string series, ProgressBar bar = null)
        {
            if (!CompressSeries) return;
            if (bar != null) bar.Message = string.Format("Compressing {0}.", series);
            ConsoleExt.Log("Compressing {0}.", series);
            var file = "";
            file = Directory.Exists(Path.Combine(SaveDirectory, "TI3 - " + series))
                       ? Path.Combine(SaveDirectory, "TI3 - " + series)
                       : Path.Combine(SaveDirectory, series);
            var fsOut = File.Create(file + ".zip");
            var zipfile = ZipFile.Create(fsOut);
            zipfile.BeginUpdate();
            AddFolderToZip(zipfile, SaveDirectory, file);
            zipfile.CommitUpdate();
            zipfile.Close();
            fsOut.Close();
            if (bar != null) bar.Message = string.Format("Compressed {0}.", series);
            ConsoleExt.Log("Compressed {0}.", series);
            Upload(file + ".zip");
        }

        static void Upload(object file)
        {
            Upload(file.ToString());
        }

        static void Upload(string file)
        {
            ConsoleExt.Log("Uploading {0}.", file);
            var nodes = Mega2.GetNodesSync();
            MegaNode nodetouse = null;
            foreach (var node in nodes.Where(node => node.Attributes.Name == "TI3 Replays"))
            {
                nodetouse = node;
            }
            var uploadnode = Mega2.UploadFileSync(nodetouse.Id, file);
            uploadnode.Attributes.Name = Path.GetFileName(file);
            Mega2.UpdateNodeAttrSync(uploadnode);
            File.AppendAllText(Path.Combine(SaveDirectory, "upload.txt"),
                               string.Format("{0} - {1}\r\n", Path.GetFileName(file),
                                             Mega.get_link(uploadnode.Id, Encode(uploadnode.NodeKey.DecryptedKey))));
            #region MegaLibrary implementation
            //Not using as will throw an exception on Mono
            /*var nodes = Mega.retrieve_nodes();
            Node nodetouse = null;
            foreach (var node in nodes.Where(node => node.attributes.name == "TI3 Replays"))
            {
                nodetouse = node;
            }
            Mega.upload(file, nodetouse == null ? Mega.create_folder("TI3 Replays") : nodetouse.identifier);
            nodes = Mega.retrieve_nodes();
            Node uploadnode = null;
            foreach (var node in nodes.Where(node => node.attributes.name == Path.GetFileName(file)))
                uploadnode = node;
            File.AppendAllText(Path.Combine(SaveDirectory, "upload.txt"),
                               string.Format("{0} - {1}\r\n", Path.GetFileName(file),
                                             Mega.get_link(uploadnode.identifier, uploadnode.key)));*/
            #endregion
            ConsoleExt.Log("Uploaded {0}.", Path.GetFileName(file));
        }

        static string Encode(byte[] data)
        {
            return Convert.ToBase64String(data)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd(new char[] { '=' });
        }

        static void AddHTML(Demo demo)
        {
            lock (HTMLRender.FormatStrings)
            {
                foreach (var key in HTMLRender.FormatStrings.Keys.ToList())
                {
                    if (key.ToLower() == demo.Series.ToLower())
                        HTMLRender.FormatStrings[key] = demo.TeamA + " vs " + demo.TeamB;
                    if (key.ToLower().StartsWith(demo.Description.ToLower()) && !HTMLRender.FormatStrings[key].Contains("vs"))
                        HTMLRender.FormatStrings[key] = "dota2://matchid=" + demo.MatchID;
                }
            }
        }

        static void AddFolderToZip(ZipFile f, string root, string folder)
        {
            string relative = folder.Substring(root.Length);

            if (relative.Length > 0)
            {
                f.AddDirectory(relative);
            }
            foreach (string file in Directory.GetFiles(folder))
            {
                relative = file.Substring(root.Length);
                ConsoleExt.Log("Adding {0}.", file);
                f.Add(file, relative);
            }

            foreach (string subFolder in Directory.GetDirectories(folder))
            {
                AddFolderToZip(f, root, subFolder);
            }
        }
    }
}
