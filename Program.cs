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
        public static string[]  NoNoWords { get; set; }
        private static CancellationTokenSource cancleStatusDisplay = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            string rootFilePath = GetRootFilePathFormUser();
            NoNoWords = GetNoNoWordsFromUser();
            Task statusDisplay = Task.Run(() => ShowDisplay(cancleStatusDisplay.Token));
            string[] files = await GetAllFilesInDirectoryAsync(rootFilePath);
            string[] ProcessedFiles = AnalyseFilesForNoNoWords(files);
            StopStatusDisplay(statusDisplay);
            SaveFilesWIthPassword(ProcessedFiles);
        }

        private static string[] AnalyseFilesForNoNoWords(string[] files)
        {
            ConcurrentBag<Task> tasks = new ConcurrentBag<Task>();
            List<string> flaggedFiles = new List<string>();
            foreach (string file in files)
                tasks.Add(Task.Run(() =>
                {
                    if (FileContainsPassword(file))
                       flaggedFiles.Add(file);
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

        private static void ShowDisplay(CancellationToken cancellation)
        {
            Console.Clear();
            Console.CursorVisible = false;
            while (!cancellation.IsCancellationRequested)
            {
                Console.SetCursorPosition(0, 0);
                WriteStatus();
                Thread.Sleep(30);
            }
            Console.Clear();
            WriteStatus();
        }

        private static string[] GetNoNoWordsFromUser()
        {
            Console.WriteLine("Enter No No words seperated with \",\"");
            return Console.ReadLine().Split(",").Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        }

        private static void SaveFilesWIthPassword(string[] files)
        {
            Console.WriteLine(string.Join("\n" ,files));
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

        private static bool FileContainsPassword(string file)
        {
            string text = File.ReadAllText(file);
            if (NoNoWords.Any(x => text.Contains(x))) return true;
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

        private static void WriteStatus()
        {
            Console.WriteLine($"Files = {FileCount}");
            Console.WriteLine($"{(int)(((double)FilesProcessed/(double)FileCount)*100)}%");
        }
    }
}
