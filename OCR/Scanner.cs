using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BoomBeachStats.OCR
{
    public class OperationAttack
    {
        public string Name { get; set; }

        public string Target { get; set; }

        public string AgoString { get; set; }

        public TimeSpan? Ago
        {
            get
            {
                TimeSpan result;
                if (!TimeSpan.TryParseExact(AgoString, new string[] { "%hh %mm", "%dd %hh" }, CultureInfo.InvariantCulture, out result)) return null;
                return result;
            }
        }

        public override string ToString()
        {
            return Name + " attacked " + Target; // TODO: On:
        }
    }
    public class Scanner
    {
        public static Scanner Instance = new Scanner();

        public string TesseractPath = @"c:\Program Files (x86)\Tesseract-OCR\tesseract.exe";
        public string ConvertPath = @"C:\Program Files (x86)\ImageMagick-6.8.6-Q16\convert.exe";


        public string ImageDir = @"C:\Users\Jared\Pictures\iCloud Photos\My Photo Stream\";


        public Dictionary<string, string> ReplacementTable = new Dictionary<string,string>();

        public Scanner()
        {
            ReplacementTable.Add("Golf4|ife00", "Golf4life00");
            ReplacementTable.Add("Go|f4|ife00", "Golf4life00");
            
            ReplacementTable.Add("de|ta18071", "delta18071");
            ReplacementTable.Add("Vikingcod", "VikingGod");
            ReplacementTable.Add("Make", "Mako");
            //ReplacementTable.Add("", "");
            //ReplacementTable.Add("", "");
            //ReplacementTable.Add("", "");
        }

        public Dictionary<string, OperationAttack> ScanForOpAttacks(string path=null)
        {
            var attacks = new Dictionary<string,OperationAttack>();
            if (path == null)
            {
                path = ImageDir + "IMG_1658.png";
            }

            var dir = Path.GetDirectoryName(path);
            var filenameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);

            var suffix = "-OpSum";

            var opSumPath = Path.Combine(dir, filenameWithoutExtension + suffix + ext);
            var opSumFileNameWithoutExtension = Path.GetFileNameWithoutExtension(opSumPath);
            var opSumPathNameWithoutExtension = Path.Combine(dir, Path.GetFileNameWithoutExtension(opSumPath));
            var opSumTxtOutput = Path.Combine(dir, opSumFileNameWithoutExtension + ".txt");


            {
                var psi = new ProcessStartInfo(ConvertPath)
                {
                    Arguments = @" """ + path + @""" -crop 580x+120+90 """ + opSumPath + @"""",

                };

                var p = Process.Start(psi);
                p.WaitForExit();

                if (p.ExitCode != 0) { MessageBox.Show("Tesseract exited with: " + p.ExitCode); }

            }

            {
                var psi = new ProcessStartInfo(TesseractPath)
                {
                    Arguments = @"-l eng """ + opSumPath + @""" """ + opSumPathNameWithoutExtension + @"""",
                };

                var p = Process.Start(psi);
                p.WaitForExit();

                if (p.ExitCode != 0) { MessageBox.Show("Tesseract exited with: " + p.ExitCode); }

            }

            File.Delete(opSumPath);

            List<string> lines = new List<string>();
            using (var tw = new StreamReader(opSumTxtOutput))
            {
                string s;
                while(( s= tw.ReadLine())!= null) lines.Add(s);
            }

            File.Delete(opSumTxtOutput);

            

            for (int i = 0; i < lines.Count;i++)
            {
                var indexOfAttacked = lines[i].IndexOf(attackedString);
                var indexOfAttackedSpace = lines[i].IndexOf(attackedStringSpace);
                string target;

                if (indexOfAttacked > 0)
                {
                    var name = lines[i].Substring(0, indexOfAttacked);

                    if (ReplacementTable.ContainsKey(name))
                    { name = ReplacementTable[name]; }

                    if (indexOfAttackedSpace > 0)
                    {
                        target = lines[i].Substring(name.Length + attackedStringSpace.Length);
                    }
                    else
                    {
                        target = lines[i + 1];
                    }

                    if (!attacks.ContainsKey(name))
                    {
                        attacks.Add(name, new OperationAttack
                            {
                                Name = name,
                                Target = target,
                            });
                    }
                }

                //if()
                //if(String.IsNullOrWhiteSpace(lines[i]))
                //{
                //    var prev = i - 1;
                //    if(prev >= 0)
                //    {
                //        if(lines[prev].EndsWith("ago"))
                //        {
                //            var prev2 = prev - 1;
                //            if(prev >= 0)
                //            {
                                
                //                var indexOfAttacked = lines[prev2].IndexOf(attackedString);
                //                if(indexOfAttacked > 0)
                //                {
                //                    var name = lines[prev2].Substring(0, indexOfAttacked);

                //                    if (ReplacementTable.ContainsKey(name))
                //                    { name =  ReplacementTable[name];}
                                    
                //                    var target = lines[prev2].Substring(name.Length + attackedString.Length);
                //                    if (!attacks.ContainsKey(name))
                //                    {
                //                        attacks.Add(name, new OperationAttack
                //                            {
                //                                Name = name,
                //                                Target = target,
                //                            });
                //                    }
                //                }
                //            }
                //        }
                //    }
                //}
            }

            return attacks;
        }
        const string attackedString = " attacked";
        const string attackedStringSpace = " attacked ";
    }
}
