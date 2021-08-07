using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using Path = System.IO.Path;
using Timer = System.Timers.Timer;

namespace CrossGameSharedPC
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static DirectoryInfo DirectoryFolder => new(Directory.GetCurrentDirectory());
        private string[] gameNames = new string[] { "Reborn", "Rejuvenation", "Desolation" };
        private string? gameName;
        private static bool HasLocalSaveGames => File.Exists(Path.Combine("Data", "Mods", "SWM - LocalSavegames.rb"));

        private string saved_games = Environment.GetEnvironmentVariable("USERPROFILE") + @"\Saved Games";
        private string filename = @"\SharedPC.rxdata";

        private string pokemon = @"\Pokemon ";

        private FileInfo[] saveFiles = Array.Empty<FileInfo>();
        private string filePath = string.Empty;

        private int latest;

        private int exitDelay = 10;

        public MainWindow()
        {
            InitializeComponent();
            //Check which game this is

            CheckCurrentGame();

            GenerateOrUpdateInfo();

            ReadSaveFiles();

            GetLatest();

            UpdateSaveFiles();

            Start();
        }

        private void CheckCurrentGame()
        {
            for (int i = 0; i < gameNames.Length; i++)
                if (DirectoryFolder.Name.Contains(gameNames[i]))
                    gameName = gameNames[i];

            _ = gameName != null
                ? StatusLog.Items.Add("Identified current game as: " + gameName)
                : StatusLog.Items.Add("Couldn't identify current game, are you sure the folder name is correct?");

            //Check if SWM - LocalSavegames exists in mod folder
            string text = "Found ";
            if (!HasLocalSaveGames)
                text = "Did not find ";

            _ = StatusLog.Items.Add(text + "LocalSaveGame mod!");
        }

        private void GenerateOrUpdateInfo()
        {
            //Create HasLocalSaveGames text file
            string? directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CrossGameSharedPC");

            filePath = Path.Combine(directory, "HasLocalSaveGames.txt");

            if (!Directory.Exists(directory))
                _ = Directory.CreateDirectory(directory);

            if (gameName == null)
                return;

            //Create file and fill in values in case it doesn't exist
            if (!File.Exists(filePath))
            {
                StreamWriter sw = File.CreateText(filePath);

                for (int i = 0; i < gameNames.Length; i++)
                {
                    sw.Write(gameNames[i] + " = ");
                    if (gameNames[i] == gameName && HasLocalSaveGames)
                    {
                        sw.WriteLine(HasLocalSaveGames);
                        sw.WriteLine(DirectoryFolder.FullName);
                    }
                    else
                        sw.WriteLine("False");
                }
                sw.Close();

                _ = StatusLog.Items.Add("Generated HasLocalSaveGames.txt in " + filePath);
            }
            //Update value of game that is launched
            else
            {
                List<string> text = File.ReadAllText(filePath).Split("\n").ToList();
                for (int i = 0; i < text.Count; i++)
                {
                    string line = text[i];
                    if (line.Substring(0, line.IndexOf(' ')).Contains(gameName))
                    {
                        text[i] = line.Substring(0, line.LastIndexOf(' ') + 1) + HasLocalSaveGames;

                        if (text.Count > i + 1 && text[i + 1].Contains(':'))
                            if (HasLocalSaveGames)
                                text[i + 1] = DirectoryFolder.FullName;
                            else
                                text.RemoveAt(i + 1);
                        else if (HasLocalSaveGames)
                            text[i] += "\n" + DirectoryFolder.FullName;


                        break;
                    }
                }
                File.WriteAllText(filePath, string.Join("\n", text));

                _ = StatusLog.Items.Add("Updated HasLocalSaveGames.txt in " + filePath);
            }
        }

        private void ReadSaveFiles()
        {
            StreamReader sr = new(filePath);
            string[] directories = new string[gameNames.Length];
            for (int i = 0; i < gameNames.Length; i++)
            {
                string? line = sr.ReadLine();

                if (line != null)
                    directories[i] = line.EndsWith("False", StringComparison.CurrentCultureIgnoreCase) ? saved_games + pokemon + gameNames[i] : sr.ReadLine() + @"\Saves";
            }
            sr.Close();

            saveFiles = new FileInfo[directories.Length];
            for (int i = 0; i < saveFiles.Length; i++)
            {
                saveFiles[i] = new(directories[i] + filename);
                _ = StatusLog.Items.Add("Checking " + gameNames[i] + " for a savefile in: " + saveFiles[i]);
            }
        }

        private void GetLatest()
        {
            List<DateTime> lastWriteTimes = new();
            for (int i = 0; i < saveFiles.Length; i++)
                lastWriteTimes.Add(saveFiles[i].Exists ? saveFiles[i].LastWriteTime : new DateTime(1, 1, 1));

            latest = lastWriteTimes.IndexOf(lastWriteTimes.Max());

            _ = StatusLog.Items.Add("The latest savefile is from the game " + gameNames[latest]);
        }

        private void UpdateSaveFiles()
        {
            for (int i = 0; i < saveFiles.Length; i++)
            {
                if (i == latest) continue; //Skip overwriting itself

                if (!saveFiles[i].Exists)
                    File.Create(saveFiles[i].FullName).Close();

                _ = saveFiles[latest].CopyTo(saveFiles[i].FullName, true);

                _ = StatusLog.Items.Add("Copied " + saveFiles[latest] + " to " + saveFiles[i].FullName);
            }
        }

        private void Start()
        {
            if (File.Exists("Game-z.exe"))
                _ = Process.Start("Game-z.exe");
            else if (File.Exists("Game.exe"))
                _ = Process.Start("Game.exe");
            else
                _ = StatusLog.Items.Add("ERROR: missing both Game-z.exe and Game.exe. One of them is required!");

            Timer timer = new(1000);

            if (exitDelay > 0)
                timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = true;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e) => Dispatcher.Invoke(DoCountDown);

        private void DoCountDown()
        {
            if (exitDelay <= 0)
                Environment.Exit(0);
            else
                _ = StatusLog.Items.Add("Closing automatically in: " + exitDelay);

            exitDelay--;
        }
    }
}
