using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PasswordFlagger
{
    class Program
    {
        public static int FilesProcessed = 0;
        public static int FileCount = 0;
        public static bool ShouldReplaceNoNoWord = false;
        private static string analysingMsg = "Analysing";
        private static string replacingMsg = "Removing NoNoWords";
        private static string root = null;

        public static string[] NoNoWordsArray { get; set; }
        private static CancellationTokenSource cancleStatusDisplay = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            KeePass keePass = new KeePass();

            string rootFilePath = string.IsNullOrEmpty(root) ? GetRootFilePathFormUser() : root;

            StatusDisplay statusDisplay = new StatusDisplay(keePass.RootFolder);
            Task status = statusDisplay.ShowDisplay();

            GetNoNosFromKP(keePass);

            status.Wait();
            if(NoNoWordsArray == null)
                NoNoWordsArray = GetNoNoWordsFromUser();

            string[] files = await GetAllFilesInDirectoryAsync(rootFilePath);
            Task statusAnalyseDisplay = Task.Run(() => ShowDisplay(cancleStatusDisplay.Token, analysingMsg, new Tuple<int, int>(0, 0)));
            string[] filesWithNoNoNames = AnalyseFilesForNoNoWords(files);
            StopStatusDisplay(statusAnalyseDisplay);
            WriteFilesWithNoNoWords(filesWithNoNoNames);

            if (filesWithNoNoNames.Length > 0 && ShouldReplaceNoNoWord)
            {
                Console.WriteLine();
                Console.WriteLine($"There are {filesWithNoNoNames.Length} files with no no words. Remove them? y/n");
                ConsoleKey input = Console.ReadKey().Key;
                
                if (!input.Equals(ConsoleKey.Y))
                    return;

                FileCount = filesWithNoNoNames.Length;
                FilesProcessed = 0;
                cancleStatusDisplay = new CancellationTokenSource();

                Task statusReplaceDisplay = Task.Run(() => ShowDisplay(cancleStatusDisplay.Token, replacingMsg, new Tuple<int, int>(0,0)));
                ReplaceNoNoWordsFromFiles(filesWithNoNoNames);
                StopStatusDisplay(statusReplaceDisplay);
            }
        }

        private static void GetNoNosFromKP(KeePass keePass)
        {
            Console.WriteLine($"Fethcing no no words...");
            NoNoWordsArray = keePass.GetCredentialsArray();
        }

        private static string[] AnalyseFilesForNoNoWords(string[] files)
        {
            ConcurrentBag<Task> tasks = new ConcurrentBag<Task>();
            List<string> flaggedFiles = new List<string>();
            foreach (string file in files)
                tasks.Add(Task.Run(() =>
                {
                    if (FileContainsNoNoWord(file)){
                        flaggedFiles.Add(file);
                    }

                    Interlocked.Increment(ref FilesProcessed);
                }));
            Task.WaitAll(tasks.ToArray());

            flaggedFiles.Sort();
            return flaggedFiles.ToArray();
        }

        private static void StopStatusDisplay(Task statusDisplay)
        {
            cancleStatusDisplay.Cancel();
            statusDisplay.Wait();
        }

        private static void ShowDisplay(CancellationToken cancellation, string msg, Tuple<int, int> position)
        {
            Console.Clear();
            Console.CursorVisible = false;
            while (!cancellation.IsCancellationRequested)
            {
                Thread.Sleep(30);
                Console.SetCursorPosition(position.Item1, position.Item2);
                WriteStatus(msg);
            }
            Console.Clear();
            WriteStatus(msg);
        }

        private static string[] GetNoNoWordsFromUser()
        {
            Console.WriteLine("Enter No No words seperated with \",\"");
            return Console.ReadLine().Split(",").Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        }

        private static void WriteFilesWithNoNoWords(string[] files)
        {
            Console.WriteLine(string.Join("\n", files));
        }

        private static async Task<string[]> GetAllFilesInDirectoryAsync(string rootFilePath)
        {
            List<Task<string[]>> moreFiles = GetTasksForGettingFilesInSubDirectories(rootFilePath);
            string[] retruntFiles = GetFiles(rootFilePath);

            foreach (Task<string[]> t in moreFiles)
            {
                string[] f = await t;
                retruntFiles = retruntFiles.Concat(f).ToArray();
            }
            return retruntFiles;
        }

        private static List<Task<string[]>> GetTasksForGettingFilesInSubDirectories(string rootFilePath)
        {
            string[] directories = GetDirectories(rootFilePath);
            List<Task<string[]>> moreFiles = new List<Task<string[]>>();
            foreach (string s in directories)
                moreFiles.Add(Task.Run(() => GetAllFilesInDirectoryAsync(s)));
            return moreFiles;
        }

        private static string GetRootFilePathFormUser()
        {
            Console.WriteLine("Enter root file path");
            String rootFilePath = Console.ReadLine();
            return rootFilePath;
        }

        private static bool FileContainsNoNoWord(string file)
        {
            string text = File.ReadAllText(file);
            if (NoNoWordsArray.Any(x => text.Contains($"{x}"))) return true;
            return false;
        }

        private static string[] GetDirectories(string rootFilePath)
        {
            return Directory.GetDirectories(rootFilePath);
        }

        private static string[] GetFiles(string rootFilePath)
        {
            string[] files = Directory.GetFiles(rootFilePath);
            Interlocked.Add(ref FileCount, files.Length);
            return files;
        }

        private static void WriteStatus(string msg)
        {
            Console.WriteLine($"File count = {FileCount}");
            Console.WriteLine($"{msg}: {(int)(((double)FilesProcessed / (double)FileCount) * 100)}%");
        }

        private static void ReplaceNoNoWordsFromFiles(string[] filePathArray)
        {
            Console.WriteLine();
            Console.WriteLine($"Replacing NoNoWords in {filePathArray.Length} files...");

            foreach (var filePath in filePathArray)
            {
                try
                {
                    ReplaceCredentials(filePath);
                    FilesProcessed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Could not replace nonoWord for file: {filePath}.\nException message: {e.Message}");
                    Console.ReadLine();
                }
            }
        }

        private static void ReplaceCredentials(string filepath)
        {
            string[] lines = File.ReadAllLines(filepath);

            for (int i = 0; i < lines.Length; i++)
            {
                //Remove nonos
                foreach (var noNoWord in NoNoWordsArray){
                    lines[i] = lines[i].Replace(noNoWord, "{1}");
                }

                //Remove User ids
                if (lines[i].Contains("User Id="))
                {
                    try
                    {
                        var dir = ConnectionstringExtensions.ConvertConnectionstringToDictionary(lines[i]);
                        if(dir.TryGetValue("User Id", out string val)){
                            lines[i] = lines[i].Replace($"User Id={val}", "User Id={0}");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Could not replace User Id for file: {filepath}.\nException message: {e.Message}");
                        Console.ReadLine();
                    }
                }
            }

            File.WriteAllLines(filepath, lines);
        }
    }
}
