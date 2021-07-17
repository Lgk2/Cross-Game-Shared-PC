using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ConsoleApp2
{
    static class Program
    {
        static void Main()
        {
            bool debug = false;

            string saved_games = Environment.GetEnvironmentVariable("USERPROFILE") + @"\Saved Games";
            string filename = @"\SharedPC.rxdata";

            string pokemon = @"\Pokemon ";

            string[] games = new string[3];
            games[0] = "Reborn";
            games[1] = "Rejuvenation";
            games[2] = "Desolation";

            string[] directories = new string[games.Length];
            for (int i = 0; i < games.Length; i++)
                directories[i] = saved_games + pokemon + games[i];

            FileInfo[] saveFiles = new FileInfo[directories.Length];
            for (int i = 0; i < saveFiles.Length; i++)
                saveFiles[i] = new(directories[i] + filename);

            List<DateTime> lastWriteTimes = new();
            for (int i = 0; i < saveFiles.Length; i++)
                lastWriteTimes.Add(saveFiles[i].Exists ? saveFiles[i].LastWriteTime : new DateTime(1, 1, 1));

            int latest = lastWriteTimes.IndexOf(lastWriteTimes.Max());

            if (debug)
                Console.WriteLine("The latest savefile is " + games[latest]);

            for (int i = 0; i < saveFiles.Length; i++)
            {
                if (i == latest) continue; //Skip overwriting itself

                saveFiles[latest].CopyTo(saveFiles[i].FullName, true);
            }   

            if (File.Exists("Game-z.exe"))
                Process.Start("Game-z.exe");
            else if (File.Exists("Game.exe"))
                Process.Start("Game.exe");
            else
            {
                Console.WriteLine("ERROR: missing both Game-z.exe and Game.exe. One of them is required!");

                if (!debug)
                    Console.ReadKey();
            }

            if (debug)
                Console.ReadKey();
        }
    }
}
