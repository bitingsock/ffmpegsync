using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ffmpegSync
{
    class ffmpegSync
    {
        static Dictionary<string, string> JobList = new Dictionary<string, string>();
        static int instances;
        static int bitrate;
        static string ffargs = "";
        static int deleted = 0;
        static void Main(string[] args)
        {
            var cfgsettings = GetConfigValues("sync.config");
            if (args.Length > 0)
            {
                foreach (var arg in args)
                {
                    var argStart = arg.Split('=')[0];
                    switch (argStart)
                    {
                        case "-s":
                        case "-source":
                            cfgsettings["source"] = arg.Split('=')[1];
                            break;
                        case "-d":
                        case "-destination":
                            cfgsettings["destination"] = arg.Split('=')[1];
                            break;
                        case "-i":
                        case "-inputformats":
                            cfgsettings["inputformats"] = arg.Split('=')[1];
                            break;
                        case "-instances":
                            cfgsettings["instances"] = arg.Split('=')[1];
                            break;
                        case "-br":
                        case "-bitrate":
                            cfgsettings["bitrate"] = arg.Split('=')[1];
                            break;
                        case "-o":
                        case "-outputformat":
                            cfgsettings["outputformat"] = arg.Split('=')[1];
                            break;
                        case "-ffargs":
                            cfgsettings["ffargs"] = arg.Split('=')[1];
                            break;
                        case "-p":
                        case "-purge":
                            cfgsettings["purge"] = arg.Split('=')[1];
                            break;
                        default:
                            cfgsettings["ffargs"] = arg;
                            break;
                    }
                }
            }
            string[] sPaths = new string[] { };
            string sourceDir = "";
            string[] dPaths;
            string destDir;
            string outputformat = "";
            string[] inputformats;
            string purgesetting = "no";
            if (cfgsettings.ContainsKey("purge"))
            {
                purgesetting = cfgsettings["purge"];
            }
            if (cfgsettings.ContainsKey("source") && Directory.Exists(cfgsettings["source"]))
            {
                sourceDir = cfgsettings["source"];
                sPaths = Directory.GetFileSystemEntries(sourceDir, "*", SearchOption.AllDirectories);
            }
            else if (cfgsettings.ContainsKey("source"))
            {
                Console.Error.WriteLine("Error: specified source does not exist");
                return;
            }
            else if (purgesetting != "only")
            {
                Console.Error.WriteLine("Error: no source specified");
                return;
            }
            if (cfgsettings.ContainsKey("destination") && Directory.Exists(cfgsettings["destination"]))
            {
                destDir = cfgsettings["destination"];
                dPaths = Directory.GetFileSystemEntries(destDir, "*", SearchOption.AllDirectories);
            }
            else if (cfgsettings.ContainsKey("destination"))
            {
                Console.Error.WriteLine("Error: specified destination does not exist");
                return;
            }
            else
            {
                Console.Error.WriteLine("Error: no destination specified");
                return;
            }
            if (cfgsettings.ContainsKey("outputformat") && cfgsettings["outputformat"] != "")
            {
                outputformat = cfgsettings["outputformat"].ToLower();
            }
            else
            {
                outputformat = "opus";
            }
            if (cfgsettings.ContainsKey("inputformats") && cfgsettings["inputformats"] != "")
            {
                inputformats = cfgsettings["inputformats"].ToLower().Split(',');
            }
            else if (purgesetting != "only")
            {
                Console.Error.WriteLine("Error: no input formats specified");
                return;
            }
            else
            {
                Console.Error.WriteLine("Error: no input formats specified");
                return;
            }
            if (cfgsettings.ContainsKey("instances"))
            {
                try
                {
                    Int32.TryParse(cfgsettings["instances"], out instances);
                }
                catch

                {
                    instances = 5;
                }
            }
            if (cfgsettings.ContainsKey("bitrate"))
            {
                try
                {
                    Int32.TryParse(cfgsettings["bitrate"], out bitrate);
                }
                catch
                {
                    bitrate = 80000;
                }
            }
            if (cfgsettings.ContainsKey("ffargs"))
            {
                ffargs = cfgsettings["ffargs"];
            }
            if (purgesetting != "no")
            {
                Console.WriteLine("purging faulty files...");
                int scanned = 0;
                int lastScanned = 0;
                Dictionary<int, Thread> probeThreads = new Dictionary<int, Thread>();
                while (scanned < dPaths.Count())
                {
                    //button HOOK
                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo key = Console.ReadKey();
                        var cursor = Console.CursorTop;
                        if (cursor == 1)
                        {
                            cursor = 2;
                        }
                        Console.SetCursorPosition(0, 1);
                        switch (key.Key)
                        {
                            case ConsoleKey.UpArrow:
                                if (instances < 25)
                                {
                                    instances++;
                                    Console.Write(new string(' ', Console.WindowWidth));
                                    Console.SetCursorPosition(0, 1);
                                    Console.WriteLine("max ffprobe instances=" + instances);
                                }
                                break;
                            case ConsoleKey.DownArrow:
                                if (instances > 0)
                                {
                                    instances--;
                                    Console.Write(new string(' ', Console.WindowWidth));
                                    Console.SetCursorPosition(0, 1);
                                    Console.WriteLine("max ffprobe instances=" + instances);
                                }
                                break;
                            default:
                                break;
                        }
                        Console.SetCursorPosition(0, cursor);
                    }
                    List<int> threadsTemp = new List<int>();
                    foreach (var thread in probeThreads)
                    {
                        try
                        {
                            if (!thread.Value.IsAlive)
                            {
                                threadsTemp.Add(thread.Value.ManagedThreadId);
                                scanned++;
                            }
                        }
                        catch (Exception err)
                        {
                            Console.Write("\r");
                            Console.WriteLine("[[could not remove probe]] " + err);
                        }
                    }
                    foreach (int id in threadsTemp)
                    {
                        if (probeThreads.ContainsKey(id))
                        {
                            probeThreads.Remove(id);
                        }
                    }
                    foreach (var file in dPaths)
                        if (probeThreads.Count < instances && File.Exists(file))
                        {
                            Thread t = new Thread(() => dPurge(file));
                            t.Start();
                            probeThreads.Add(t.ManagedThreadId, t);
                        }
                    if (scanned - lastScanned > 10)
                    {
                        Console.Write("\r");
                        Console.Write("Faulty files deleted: " + deleted + " of " + scanned + " scanned");
                        lastScanned = scanned;
                    }
                }
                dPaths = Directory.GetFileSystemEntries(destDir, "*", SearchOption.AllDirectories);
            }
            if (purgesetting == "only")
            {
                Console.WriteLine("purge=only, exit");
                return;
            }
            Console.WriteLine("[1/4] comparing files...");
            var destRelDir = dPaths.Select(r => r.Replace(destDir, "")).ToArray();
            var sourceRelDir = sPaths.Select(r => r.Replace(sourceDir, "")).ToArray();
            var sourceRelDirNoExt = new List<string>();
            foreach (var sfile in sourceRelDir)
            {
                var sfileExt = sfile.Substring(sfile.LastIndexOf('.') + 1);
                var index = sfile.LastIndexOf(sfileExt);
                if (index >= 0)
                {
                    sourceRelDirNoExt.Add(sfile.Substring(0, index));
                }
            }
            var toDelete = destRelDir.Where(r => !sourceRelDir.Contains(r)).ToList();
            Console.WriteLine("[2/4] DELETING files not in source...");
            foreach (var dFile in toDelete)
            {
                var dFileExt = dFile.Substring(dFile.LastIndexOf('.') + 1).ToLower();
                string dFileNoExt = "";
                var index = dFile.LastIndexOf(dFileExt);
                if (index >= 0)
                {
                    dFileNoExt = dFile.Substring(0, index);
                }
                if (File.Exists(destDir + dFile) && (outputformat != dFileExt || !sourceRelDirNoExt.Contains(dFileNoExt)))
                {
                    File.Delete(destDir + dFile);
                    Console.WriteLine("delete: " + dFile);
                }
                else if (Directory.Exists(destDir + dFile))
                {
                    Directory.Delete(destDir + dFile, true);
                    Console.WriteLine("delete: " + dFile);
                }
            }
            Console.WriteLine("[3/4] adding jobs and copying...");
            dPaths = Directory.GetFileSystemEntries(destDir, "*", SearchOption.AllDirectories);
            destRelDir = dPaths.Select(r => r.Replace(destDir, "")).ToArray();
            var toConvert = sourceRelDir.Except(destRelDir);
            foreach (var sFile in toConvert)
            {
                if (Directory.Exists(sourceDir + sFile))
                {
                    Directory.CreateDirectory(destDir + sFile);
                }
                else
                {
                    var sFileExt = sFile.Substring(sFile.LastIndexOf('.') + 1);
                    string sFileNoExt = "";
                    var index = sFile.LastIndexOf(sFileExt);
                    if (index >= 0)
                    {
                        sFileNoExt = sFile.Substring(0, index);
                    }
                    if (inputformats.Contains(sFileExt.ToLower()) && !destRelDir.Contains(sFileNoExt + outputformat))
                    {
                        JobList.Add(sourceDir + sFile, destDir + sFileNoExt + outputformat);
                        Console.WriteLine("Add job: " + sFile);
                    }
                    else if (!destRelDir.Contains(sFileNoExt + outputformat))
                    {
                        try
                        {
                            Console.WriteLine("Copy: " + sFile);
                            File.Copy(sourceDir + sFile, destDir + sFile);
                        }
                        catch (Exception delEx)
                        {
                            Console.Write("\r");
                            Console.Write("--Warning: Could not create: " + destDir + sFile + " (" + delEx.Message + ")");
                        }
                    }
                }
            }
            int numJobs = JobList.Count;
            int completeJobs = 0;
            int lastinstances = instances;
            Dictionary<int, Thread> threads = new Dictionary<int, Thread>();
            Console.WriteLine("[4/4] converting...");
            while (JobList.Count > 0 || threads.Count > 0)
            {
                if (Console.KeyAvailable)//button HOOK
                {
                    ConsoleKeyInfo key = Console.ReadKey();
                    switch (key.Key)
                    {
                        case ConsoleKey.UpArrow:
                            if (instances < 25)
                            {
                                instances++;
                                Console.WriteLine("max ffmpeg instances=" + instances);
                            }
                            break;
                        case ConsoleKey.DownArrow:
                            if (instances > 0)
                            {
                                instances--;
                                Console.WriteLine("max ffmpeg instances=" + instances);
                            }
                            break;
                        default:
                            break;
                    }
                    lastinstances = instances;
                }
                List<int> threadsTemp = new List<int>();
                foreach (var thread in threads)
                {
                    try
                    {
                        if (!thread.Value.IsAlive)
                        {
                            threadsTemp.Add(thread.Value.ManagedThreadId);
                            completeJobs++;
                            Console.Write("\r");
                            Console.WriteLine("Conversions complete: " + completeJobs + " of " + numJobs);
                        }
                    }
                    catch (Exception err)
                    {
                        Console.Write("\r");
                        Console.WriteLine("could not remove job " + err);
                    }
                }
                foreach (int id in threadsTemp)
                {
                    if (threads.ContainsKey(id))
                    {
                        threads.Remove(id);
                    }
                }
                if (threads.Count < instances && JobList.Count > 0)
                {
                    Thread t = new Thread(() => Convert());
                    t.Start();
                    threads.Add(t.ManagedThreadId, t);
                }
                Thread.Sleep(10);
            }
            Console.WriteLine("All conversions/transfers complete.");
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
        static Dictionary<string, string> GetConfigValues(string inCFGFilePath)
        {
            var lines = File.ReadAllLines(inCFGFilePath);
            Dictionary<string, string> rVal = new Dictionary<string, string>();
            foreach (var line in lines)
            {
                var splt = line.Split(new char[] { '=' });
                rVal.Add(splt[0], splt[1]);
            }
            return rVal;
        }
        static Boolean dPurge(string path)
        {
            Process p = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.Arguments = "-loglevel 24 " + "\"" + path + "\"";
            startInfo.FileName = "ffprobe.exe";
            p.StartInfo = startInfo;
            p.Start();
            string Err = p.StandardError.ReadToEnd();
            if (Err != "")
            {
                Console.WriteLine(Err + "#########" + path);
                try
                {
                    File.Delete(path);
                    deleted++;
                }
                catch (Exception delEx)
                {
                    Console.Write("\r");
                    Console.Write("--Warning: Could not purge: " + path + " (" + delEx.Message + ")");
                    p.Close();
                    return false;
                }
                p.Close();
                return true;
            }
            p.Close();
            return false;
        }
        static void Convert()
        {
            Process p = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            var job = JobList.First();
            string argu = "";
            if (ffargs != "" && ffargs != "\"\"")
            {
                argu += ffargs + " ";
            }
            argu += "-i " + '"' + @job.Key.ToString() + '"' + " -b:a " + bitrate + " " + '"' + @job.Value.ToString() + '"';
            startInfo.Arguments = argu;
            startInfo.FileName = "ffmpeg.exe";
            p.StartInfo = startInfo;
            p.Start();
            JobList.Remove(job.Key);
            p.WaitForExit();
        }
    }
}