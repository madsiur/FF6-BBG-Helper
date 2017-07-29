// Author: Frederic Dupont <themadsiur@gmail.com>
// Copyright 2017 Frederic Dupont

// This file is part of IMPORTER (FF6 BBG Helper).

// IMPORTER is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// IMPORTER is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with IMPORTER.  If not, see<http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace imppal
{
    class Program
    {
        static void Main(string[] args)
        {
            string folder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string filter = "*.smc";
            string[] files = Directory.GetFiles(folder, filter);

            if (files.Length > 0)
            {
                string fileName = files[0];
                FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite);
                BinaryReader br = new BinaryReader(fs);
                BinaryWriter bw = new BinaryWriter(fs);

                Console.WriteLine("Opening ROM...");
                
                Regex fileNameRegex = new Regex(@"BG_[A-F0-9]{2}");
                List<string> palFiles = Directory.GetFiles(folder, "*.txt").Where(path => fileNameRegex.IsMatch(path)).ToList();

                if (palFiles.Count > 0)
                {
                    Console.WriteLine("Found " + palFiles.Count + " palette files...");
                    Console.WriteLine(" ");

                    Regex colRegex = new Regex(@"#[a-f0-9]{6}");
                    bool error = false;

                    for (int i = 0; i < palFiles.Count && !error; i++)
                    {
                        string palFileName = Path.GetFileName(palFiles[i]);
                        FileStream pfs = new FileStream(palFiles[i], FileMode.Open, FileAccess.Read);
                        StreamReader psr = new StreamReader(pfs);

                        byte[] palette = new byte[96];
                        int lnCounter = 0;

                        while (psr.Peek() >= 0 && !error && lnCounter < 48)
                        {
                            string ln = psr.ReadLine();

                            Match match = colRegex.Match(ln);

                            if (match.Success)
                            {
                                string strColor = match.Value;

                                int c = Convert.ToInt32(strColor.Substring(1, 6), 16);
                                int r = (c & 0xF80000) >> 19;
                                int g = (c & 0x00F800) >> 6;
                                int b = (c & 0x0000F8) << 7;
                                int col =  b | g | r;
                                palette[lnCounter * 2] = (byte)(col & 0x00FF);
                                palette[lnCounter * 2 + 1] = (byte)((col & 0xFF00) >> 8);

                                lnCounter++;
                            }
                            else
                            {
                                error = true;
                                Console.WriteLine("Error in " + palFileName + " on line " + lnCounter + ". Color string with format #RRGGBB expected.");
                                Console.WriteLine("Import failed.");
                                Console.ReadKey();
                            }
                        }

                        int bgIndex = Convert.ToInt16(palFileName.Substring(3, 2), 16);

                        br.BaseStream.Seek(0x270000 + (bgIndex * 6 + 5), SeekOrigin.Begin);
                        byte palIndex = (byte)(br.ReadByte() & 0x7F);

                        bw.BaseStream.Seek(0x270150 + (palIndex * 96), SeekOrigin.Begin);
                        bw.Write(palette);

                        psr.Close();
                        pfs.Close();

                        Console.WriteLine("writing " + palFileName + " to background palette " + palIndex.ToString("X2") + ".");
                    }

                    bw.Close();
                    br.Close();
                    fs.Close();

                    Console.WriteLine(" ");
                    Console.WriteLine("Import succesfull!");
                    Console.ReadKey();
                }
                else
                {
                    Console.WriteLine(" ");
                    Console.WriteLine("No palette file found (.txt) in current folder!");
                    Console.WriteLine("Import failed.");
                    Console.ReadKey();
                }
            }
            else
            {
                Console.WriteLine(" ");
                Console.WriteLine("No ROM found (.smc) in current folder!");
                Console.WriteLine("Import failed.");
                Console.ReadKey();
            }
        }
    }
}
