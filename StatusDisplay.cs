using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PasswordFlagger
{
    public class StatusDisplay
    {
        IStatusRepporter statusRepporter;
        List<IStatusObject> statusObjectRecord = new List<IStatusObject>();
        public bool ProcessDone { get; set; } = false;

        public StatusDisplay(IStatusRepporter statusRepporter)
        {
            this.statusRepporter = statusRepporter;
            statusRepporter.ProcessStarted += new EventHandler(ProcessStarted);
        }

        public async Task ShowDisplay()
        {
            foreach (IStatusObject statusObject in statusRepporter.GetStatusObjects())
            {
                statusObjectRecord.Add(statusObject);
                DrawStatusObject(statusObject);
                statusObject.ObjectCompleted += new EventHandler(ObjectCompleted);
            }
            await Task.Yield();
            while (!ProcessDone)
            {
                Task.Delay(25);
            }
        }

        private void DrawStatusObject(IStatusObject statusObject)
        {
            int line = statusObjectRecord.IndexOf(statusObject);
            Console.SetCursorPosition(0, line);
            ClearCurrentConsoleLine();
            if (statusObject.IsCompleted)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"+ {statusObject.GetName()}");
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"- {statusObject.GetName()}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        private void ProcessStarted(object sender, EventArgs e)
        {
            foreach (IStatusObject statusObject in statusRepporter.GetStatusObjects())
            {
                statusObject.ObjectCompleted += new EventHandler(ObjectCompleted);
            }
        }

        private void ObjectCompleted (object sender, EventArgs e)
        {
            if (statusRepporter.GetStatusObjects().Any(x => !x.IsCompleted))
                DrawStatusObject(sender as IStatusObject);
            else
                EndDisplay();
        }

        private void EndDisplay()
        {
            ProcessDone = true;
        }

        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }
    }
}
