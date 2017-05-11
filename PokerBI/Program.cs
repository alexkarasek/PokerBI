using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Configuration;

namespace PokerBI
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Game On...");

            string inputpath = ConfigurationManager.AppSettings["LogFilePath"];
            string archivepath = ConfigurationManager.AppSettings["LogFileArchivePath"];
            string Outputfile = ConfigurationManager.AppSettings["OutputFile"];
            string RefreshAll = ConfigurationManager.AppSettings["RefreshAll"];

            if (RefreshAll == "Y")
            {
                using (StreamWriter outputFile = new StreamWriter(Outputfile, false))
                {
                    outputFile.WriteLine("Game" + "|" + "Date" + "|" + "Player" + "|" + "Action" + "|" + "Cards" + "|" + "Amount" + "|" + "Street", false);
                }
            }

            string[] fileEntries = Directory.GetFiles(inputpath);
            foreach (string fileName in fileEntries)
            {
                string archivedfile_in = archivepath + @"\" + Path.GetFileName(fileName);
                string inputfile_in = inputpath + @"\" + Path.GetFileName(fileName);

                Console.WriteLine("Processing File: " + inputfile_in);
                
                FileParser(ref inputfile_in, ref archivedfile_in);
            }
            Console.WriteLine("Done Processing Files....");
            Console.ReadLine();

        }
        static void FileParser(ref string inputfile, ref string archivedfile)
        {
            try
            {
                string street = "";
                string game = "";
                string date = "";

               string line_in;

               using (StreamReader sr = new StreamReader(inputfile))

                    while ((line_in = sr.ReadLine()) != null)
                    {
                        if (line_in != @"*** SHOW DOWN ***" && line_in != @"*** SUMMARY ***") 
                        {
                            LineParser(ref line_in, ref street, ref game, ref date);
                        }
                    }
                if(File.Exists(archivedfile))
                {
                    File.Delete(archivedfile);
                }

                File.Move(inputfile, archivedfile);
            }
            catch (Exception e)
            {
                // Let the user know what went wrong.
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                Console.ReadLine();

            }
        }

        static void LineParser(ref string line, ref string street, ref string game, ref string date)
        {
            try { 
            List<string> patterns = new List<string>();
                patterns.Add(@"Dealt to\[.\D\s.\D\]");   //Deal
                patterns.Add(@"^PokerStars\s");  //New Game
                patterns.Add(@"\sbets\s");  //Bet
                patterns.Add(@"\sposts\s\S+\sblind");  //Blind
                patterns.Add(@"\sfolds\s");   //Fold
                patterns.Add(@"\scalls\s");  //Call
                patterns.Add(@"\scollected\s[0-9]");  //Win
                patterns.Add(@"\schecks\s");  //Check
                patterns.Add(@"\sposts\sthe\sante");  //Ante
                patterns.Add(@"\*\*\*\s.+\s\*\*\*");  //New Street
                patterns.Add(@".+\:\sraises+.+[0-9]+\sto\s[0-9]+");  //Raise

                string Outputfile = ConfigurationManager.AppSettings["OutputFile"];

                using (StreamWriter outputFile2 = new StreamWriter(Outputfile, true))
                { 
                    string action = "";
                    string cards = "";
                    string player = "";
                    string amount = "";

                    for (int i = 0;i < patterns.Count; i++)
                    {
                            cards = @"[]";
                            
                            Regex rgx = new Regex(patterns[i], RegexOptions.IgnoreCase);
                            MatchCollection matches = rgx.Matches(line);
                            if (matches.Count > 0)
                            {
                                Regex rgxStage = new Regex(@"");
                                MatchCollection Stage = rgxStage.Matches(line);

                                switch (i)
                                {
                                    case 0:  //DEAL
                                        if (line.IndexOf("SHOWS") == -1 && line.IndexOf("shows") == -1 && line.IndexOf(" showed ") == -1 && line.IndexOf(" mucked ") == -1)
                                        {

                                            cards = line.Substring(line.IndexOf("["), 7);
                                            player = line.Substring(9, line.IndexOf("[") - 10);
                                            street = "PRE-FLOP";
                                        }
                                        break;

                                    case 1:  //NEW GAME
                                        game = line.Substring(27, line.IndexOf(":") - 27);
                                        action = "Info";
                                        rgxStage = new Regex(@"\d{4}/\d{2}/\d{2}");
                                        Stage = rgxStage.Matches(line);
                                        date = Stage[0].Value;
                                        street = "PRE-FLOP";
                                        break;

                                    case 2:  //BET
                                        rgxStage = new Regex(@"bets\s\d+");
                                        Stage = rgxStage.Matches(line);
                                        amount = Stage[0].Value.Substring(5, Stage[0].Length - 5);
                                        action = "bet";
                                        player = line.Substring(0, line.IndexOf(":"));
                                        break;

                                    case 3:  //BLIND
                                        rgxStage = new Regex(@"\s\d+");
                                        Stage = rgxStage.Matches(line);
                                        action = "blind";
                                        amount = Stage[0].Value.Substring(1, Stage[0].Length - 1);
                                        player = line.Substring(0, line.IndexOf(":"));
                                        break;

                                    case 4:   //FOLD
                                        action = "fold";
                                        amount = "0";
                                        player = line.Substring(0, line.IndexOf(":"));
                                        break;

                                    case 5:  //CALL
                                        rgxStage = new Regex(@"\s\d+");
                                        Stage = rgxStage.Matches(line);
                                        action = "call";
                                        amount = Stage[0].Value.Substring(1, Stage[0].Length - 1);
                                        player = line.Substring(0, line.IndexOf(":"));
                                        break;

                                    case 6:  //WIN
                                        rgxStage = new Regex (@"\d+\sfrom\s");
                                        Stage = rgxStage.Matches(line);
                                        action = "win";
                                        amount = Stage[0].Value.Substring(0, Stage[0].Length - 6);
                                        rgxStage = new Regex(@".+\scollect");
                                            if (line.IndexOf(@": ") > 0)
                                            {
                                                player = line.Substring(0, line.IndexOf(": collect"));
                                            }
                                        player = line.Substring(0, line.IndexOf(" collect") );
                                        break;

                                    case 7:  //CHECK
                                        action = "check";
                                        amount = "0";
                                        player = line.Substring(0, line.IndexOf(":"));
                                        break;

                                    case 8:  //ANTE
                                        rgxStage = new Regex(@"ante\s\d+");
                                        Stage = rgxStage.Matches(line);
                                        action = "ante";
                                        amount = Stage[0].Value.Substring(5, Stage[0].Length - 5);
                                        player = line.Substring(0, line.IndexOf(@":"));
                                        break;

                                    case 9:  //NEW STREET
                                        action = "Info";
                                            street = line.Substring(4, line.IndexOf("***", 4) - 5);
                                            if(street == "HOLE CARDS")
                                            {
                                        street = "PRE-FLOP";
                                            }
                                        break;

                                    case 10:  //RAISE
                                        rgxStage = new Regex(@"\s\d+\s");
                                        Stage = rgxStage.Matches(line);
                                        action = "raise";
                                        amount = Stage[0].Value.Substring(1, Stage[0].Length - 2);
                                        player = line.Substring(0, line.IndexOf(":"));
                                        break;


                                }
                                    if (action!="Info" )
                                        {
                                        outputFile2.WriteLine(game + "|" + date + "|" + player + "|" + action + "|" + cards + "|" + amount + "|" + street);

                                    }
                                }
                            }
                    outputFile2.Close();
                }

            }
            catch (Exception e)
            {
                // Let the user know what went wrong.
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                Console.ReadLine();

            }
        }
    }


}
