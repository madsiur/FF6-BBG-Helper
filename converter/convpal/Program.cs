// Author: Frederic Dupont <themadsiur@gmail.com>
// Copyright 2017 Frederic Dupont

// This file is part of CONVERTER (FF6 BBG Helper).

// CONVERTER is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// CONVERTER is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with CONVERTER.  If not, see<http://www.gnu.org/licenses/>.

using convpal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace convpal
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch s1 = Stopwatch.StartNew();

            string folder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            Regex bgNameRegex = new Regex(@"BG_[A-F0-9]{2}");
            Regex palNameRegex = new Regex(@"BG_[A-F0-9]{2}_PAL_[A-F0-9]{2}");
            List<string> bgFiles = Directory.GetFiles(folder, "*.png").Where(path => bgNameRegex.IsMatch(path)).ToList();
            List<string> palFiles = Directory.GetFiles(folder, "*.pal").Where(path => palNameRegex.IsMatch(path)).ToList();
            bool error = false;
            bool finalErr = false;
            string message = string.Empty;
            string saveFolderfinal = Path.Combine(folder, "final");
            string filename = string.Empty;
            string savePath = string.Empty;
            string relSrcPath = string.Empty;
            string relSavePath = string.Empty;

            Stopwatch s = Stopwatch.StartNew();
            Console.WriteLine(" ");
            
            if (bgFiles.Count > 0)
            {
                if (!Directory.Exists(saveFolderfinal))
                {
                    try
                    {
                        Directory.CreateDirectory(saveFolderfinal);
                    }
                    catch
                    {
                        error = true;
                        message = "Error creating directory " + saveFolderfinal;
                    }
                }

                if (!error)
                {
                    foreach (string file in bgFiles)
                    {
                        Bitmap src = new Bitmap(file);
                        Bitmap dest = null;
                        string palFile = string.Empty;

                        if (src.PixelFormat != PixelFormat.Format8bppIndexed)
                        {
                            try
                            {
                                dest = CopyToBpp(src);
                            }
                            catch
                            {
                                error = true;
                                message = "Error converting " + filename + " to 8bpp";
                            }
                        }
                        else
                        {
                            dest = src;
                        }

                        try
                        {
                            palFile = palFiles.First(name => name.Contains(Path.GetFileNameWithoutExtension(file)));
                        }
                        catch
                        {
                            error = true;
                            message = "No palette file " + Path.GetFileNameWithoutExtension(file) + "_PAL_XX.pal match background image " + Path.GetFileName(file);
                        }

                        FileStream fs = null;
                        BinaryReader br = null;
                        Color[] ff6Pal = null;
                        string finalFileName = Path.GetFileName(file);
                        string palFileName = Path.GetFileName(palFile);
                        Color black = Color.FromArgb(0, 0, 0);

                        if (!error)
                        {
                            ff6Pal = new Color[256];
                            savePath = Path.Combine(saveFolderfinal, finalFileName);
                            relSavePath = Path.Combine(Path.GetFileName(Path.GetDirectoryName(savePath)), Path.GetFileName(savePath));
                            relSrcPath = Path.Combine(Path.GetFileName(Path.GetDirectoryName(file)), Path.GetFileName(file));

                            try
                            {
                                fs = new FileStream(palFile, FileMode.Open, FileAccess.Read);
                                br = new BinaryReader(fs);
                            }
                            catch
                            {
                                error = true;
                                message = "Cannot open palette file " + palFileName;
                            }
                        }

                        if (!error)
                        {
                            try
                            {
                                br.BaseStream.Seek(24, SeekOrigin.Begin);

                                for (int i = 0; i < 192; i += 4)
                                {
                                    byte r = br.ReadByte();
                                    byte g = br.ReadByte();
                                    byte b = br.ReadByte();
                                    br.ReadByte();

                                    ff6Pal[i / 4] = Color.FromArgb(r, g, b);
                                }

                                for (int i = 48; i < 256; i++)
                                {
                                    ff6Pal[i] = black;
                                }

                                message = "Reading palette file " + palFileName;
                            }
                            catch (Exception e)
                            {
                                error = true;
                                message = "Error reading palette file " + ":\n" + e.Message;
                            }
                        }

                        Console.WriteLine(message);

                        Bitmap destFinal = null;
                        LockBitmap srcLock = null;
                        LockBitmap destLock = null;

                        if (!error)
                        {
                            try
                            {
                                destFinal = new Bitmap(dest.Width, dest.Height, PixelFormat.Format8bppIndexed);
                                List<Color> ff6palList = ff6Pal.ToList();
                                byte[] palMap = new byte[dest.Palette.Entries.Length];

                                for (int i = 0; i < dest.Palette.Entries.Length; i++)
                                {
                                    Color c = dest.Palette.Entries[i];
                                    byte index = (byte)ff6palList.IndexOf(c);
                                    palMap[i] = index;
                                }
                                if (!error)
                                {
                                    ColorPalette pal = dest.Palette;

                                    for (int i = 0; i < 256; i++)
                                    {
                                        pal.Entries[i] = ff6Pal[i];
                                    }

                                    destFinal.Palette = pal;
                                    srcLock = new LockBitmap(dest);
                                    destLock = new LockBitmap(destFinal);
                                    srcLock.LockBits();
                                    destLock.LockBits();

                                    List<int> tempPal = new List<int>();
                                    Dictionary<int, int> dict = new Dictionary<int, int>();

                                    int h = src.Height;
                                    int w = src.Width;

                                    for (int y = 0; y < h; y++)
                                    {
                                        for (int x = 0; x < w; x++)
                                        {
                                            byte b1 = srcLock.GetPixel(y * w + x);
                                            destLock.Pixels[y * w + x] = palMap[b1];
                                        }
                                    }

                                    srcLock.UnlockBits();
                                    destLock.UnlockBits();
                                }
                            }
                            catch (Exception e)
                            {
                                error = true;
                                message = "Error converting palette from " + relSrcPath + " to " + relSavePath + ". Error:\n" + e.Message;
                            }
                        }

                        if (!error)
                        {
                            try
                            {
                                destFinal.Save(savePath, ImageFormat.Png);
                                destFinal.Dispose();
                            }
                            catch
                            {
                                error = true;
                                message = "Error saving final 8bpp PNG " + relSavePath;
                            }
                        }

                        if (!error)
                        {
                            Console.WriteLine("Saving 8bpp final image as " + relSavePath + ".");
                        }
                        else
                        {
                            Console.WriteLine(message);
                            finalErr = true;
                        }
                    }
                }
                else
                {
                    Console.WriteLine(message);
                    finalErr = true;
                }

                Console.WriteLine(" ");

                if (!finalErr)
                {
                    Console.WriteLine("Operation is succesfull!");
                }
                else
                {
                    Console.WriteLine("You have one or more error(s)!");
                }

                s.Stop();
                Console.WriteLine(" ");
                Console.WriteLine("Time taken: {0}ms", s.ElapsedMilliseconds);
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine(" ");
                Console.WriteLine("No background file found in the application folder to continue operations..");
            }

        }

        static Bitmap CopyToBpp(Bitmap b)
        {
            int h = b.Height;
            int w = b.Width;
            Color[] pal = new Color[256];
            Color black = Color.FromArgb(0, 0, 0);
            LockBitmap lb = new LockBitmap(b);
            lb.LockBits();
            int end = 0;

            for (int y = 0; y < h && end < 48; y++)
            {
                for (int x = 0; x < w && end < 48; x++)
                {
                    Color c1 = lb.GetPixel(x, y);
                    Color c2 = Color.FromArgb(c1.R, c1.G, c1.B);

                    if (!pal.Contains(c2))
                    {
                        pal[end] = c2;
                        end++;
                    }
                }
            }

            for(int i = end; i < 256; i++)
            {
                pal[i] = black;
            }

            Bitmap dest = new Bitmap(w, h, PixelFormat.Format8bppIndexed);
            ColorPalette palette = dest.Palette;

            for (int i = 0; i < 256; i++)
            {
                palette.Entries[i] = pal[i];
            }

            dest.Palette = palette;

            LockBitmap lDest = new LockBitmap(dest);
            lDest.LockBits();

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    lDest.Pixels[y * w + x] = (byte)Array.IndexOf(pal, lb.GetPixel(x, y));
                }
            }

            lDest.UnlockBits();
            lb.UnlockBits();

            return dest;
        }
    }
}
