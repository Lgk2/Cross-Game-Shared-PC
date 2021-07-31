using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;

namespace ConsoleApp2
{
    class Program
    {
        private int thisGameIndex;
        private string[] gameNames = new string[] { "Reborn", "Rejuvenation", "Desolation" };
        private bool hasLocalSaveGames;
        private DirectoryInfo directoryFolder;
        private string game;

        private string saved_games = Environment.GetEnvironmentVariable("USERPROFILE") + @"\Saved Games";
        private string filename = @"\SharedPC.rxdata";

        private string pokemon = @"\Pokemon ";

        private FileInfo[] saveFiles;

        private int latest;

        private string filePath;

        static void Main()
        {
            Program prog = new();

            //Check which game this is
            prog.CheckCurrentGame();

            prog.GenerateOrUpdateInfo();

            prog.ReadSaveFiles();

            prog.GetLatest();

            prog.UpdateSaveFiles();

            prog.Start();
        }

        private void CheckCurrentGame()
        {
            directoryFolder = new(Directory.GetCurrentDirectory());
            string directoryFolderName = directoryFolder.Name;
            game = gameNames.First(c => directoryFolderName.Contains(c));
            thisGameIndex = Array.IndexOf(gameNames, game);

            Console.WriteLine("Identified current game as: " + game);

            //Check if SWM - LocalSavegames exists in mod folder
            hasLocalSaveGames = File.Exists(Path.Combine("Data", "Mods", "SWM - LocalSavegames.rb"));
            string text = "Found ";
            if (!hasLocalSaveGames)
                text = "Did not find ";

            Console.WriteLine(text + "LocalSaveGame mod!");
        }

        private void GenerateOrUpdateInfo()
        {
            //Create HasLocalSaveGames text file
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CrossGameSharedPC");
            filePath = Path.Combine(directory, "HasLocalSaveGames.txt");

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            //Create file and fill in values in case it doesn't exist
            if (!File.Exists(filePath))
            {
                StreamWriter sw = File.CreateText(filePath);

                for (int i = 0; i < gameNames.Length; i++)
                {
                    sw.Write(gameNames[i] + " = ");
                    if (i == thisGameIndex && hasLocalSaveGames)
                    {
                        sw.WriteLine(hasLocalSaveGames);
                        sw.WriteLine(directoryFolder.FullName);
                    }
                    else
                        sw.WriteLine("False");
                }
                sw.Close();

                Console.WriteLine("Generated HasLocalSaveGames.txt in " + filePath);
            }
            //Update value of game that is launched
            else
            {
                List<string> text = File.ReadAllText(filePath).Split("\n").ToList();
                for (int i = 0; i < text.Count; i++)
                {
                    string line = text[i];
                    if (line.Substring(0, line.IndexOf(' ')).Contains(game))
                    {
                        text[i] = line.Substring(0, line.LastIndexOf(' ') + 1) + hasLocalSaveGames;

                        if (text.Count > i + 1 && text[i + 1].Contains(':'))
                            if (hasLocalSaveGames)
                                text[i + 1] = directoryFolder.FullName;
                            else
                                text.RemoveAt(i + 1);
                        else if (hasLocalSaveGames)
                            text[i] += "\n" + directoryFolder.FullName;


                        break;
                    }
                }
                File.WriteAllText(filePath, string.Join("\n", text));

                Console.WriteLine("Updated HasLocalSaveGames.txt in " + filePath);
            }
        }

        private void ReadSaveFiles()
        {
            StreamReader sr = new(filePath);
            string[] directories = new string[gameNames.Length];
            for (int i = 0; i < gameNames.Length; i++) //TODO take into account hasLocalSaveGames and read directory
            {
                string line = sr.ReadLine();

                directories[i] = line.EndsWith("False") ? saved_games + pokemon + gameNames[i] : sr.ReadLine() + @"\Saves";
            }
            sr.Close();

            saveFiles = new FileInfo[directories.Length];
            for (int i = 0; i < saveFiles.Length; i++)
            {
                saveFiles[i] = new(directories[i] + filename);
                Console.WriteLine("Checking " + gameNames[i] + " for a savefile in: " + saveFiles[i]);
            }
        }

        private void UpdateSaveFiles()
        {
            for (int i = 0; i < saveFiles.Length; i++)
            {
                if (i == latest) continue; //Skip overwriting itself

                saveFiles[latest].CopyTo(saveFiles[i].FullName, true);
                Console.WriteLine("Copied " + saveFiles[latest] + " to " + saveFiles[i].FullName);
            }
        }

        private void GetLatest()
        {
            List<DateTime> lastWriteTimes = new();
            for (int i = 0; i < saveFiles.Length; i++)
                lastWriteTimes.Add(saveFiles[i].Exists ? saveFiles[i].LastWriteTime : new DateTime(1, 1, 1));

            latest = lastWriteTimes.IndexOf(lastWriteTimes.Max());

            Console.WriteLine("The latest savefile is from the game " + gameNames[latest]);
        }

        private int secondsRemaining = 5;
        private Timer timer;

        private void Start()
        {
            if (File.Exists("Game-z.exe"))
                Process.Start("Game-z.exe");
            else if (File.Exists("Game.exe"))
                Process.Start("Game.exe");
            else
                Console.WriteLine("ERROR: missing both Game-z.exe and Game.exe. One of them is required!");

            timer = new(1000);
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = true;
            timer.Start();
            Console.ReadKey();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (secondsRemaining == 0)
                Environment.Exit(0);
            else
                Console.WriteLine("Closing automatically in: " + secondsRemaining);

            secondsRemaining--;
        }
    }
}
