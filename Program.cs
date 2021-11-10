using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PasswordFlagger
{
    class Program
    {
        private static readonly object _lockObj = new object();
        public static int TasksRunning { get; set; } = 0;
        public static int TasksDone { get; set; } = 0;
        public static int FileCount { get; set; } = 0;
        public static string[]  NoNoWords { get; set; }
        static async Task Main(string[] args)
        {
            string rootFilePath = GetRootFilePathFormUser();
            NoNoWords = GetNoNoWordsFromUser();
            Task statusDisplay = Task.Run(() => ShowDisplay());
            string[] files = await GetAllFilesInDirectoryAsync(rootFilePath);
            statusDisplay.Dispose();
            SaveFilesWIthPassword(files);
        }

        private static Action ShowDisplay()
        {
            Console.Clear();
            while (true)
            {
                Console.SetCursorPosition(0, 0);
                Console.WriteLine($"Files = {FileCount}");

                Thread.Sleep(200);
            }
        }

        private static string[] GetNoNoWordsFromUser()
        {
            Console.WriteLine("Enter No No words seperated with \",\"");
            return Console.ReadLine().Split(",").Select(x => x.Trim()).ToArray();
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
                moreFiles.Add(GetAllFilesInDirectoryAsync(s));
            return moreFiles;
        }

        private static string[] GetArrayOfFilesWithPassword(string[] files)
        {
            string[] retruntFiles;
            List<string> fileList = new List<string>();
            foreach (string s in files)
                if (FileContainsPassword(s)) fileList.Add(s);
            retruntFiles = fileList.ToArray();
            return retruntFiles;
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
            FileCount += files.Length;
            return files;
        }

        private static void RegistreNewTask()
        {
                TasksRunning++;
                WriteStatus();
        }

        private static void WriteStatus()
        {
            Console.WriteLine($"{TasksRunning}\t{TasksDone}");
        }

        private static void RegistreCompleteTask()
        {
                TasksRunning--;
                TasksDone++;
                WriteStatus();
        }
    }
}
