// Author: Frederic Dupont <themadsiur@gmail.com>
// Copyright 2017 Frederic Dupont

// This file is part of EXPORTER (FF6 BBG palette Helper).

// EXPORTER is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// EXPORTER is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with FF6MMGEN.  If not, see<http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.IO;

namespace palext
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
                Console.WriteLine(" ");

                string fileName = files[0];
                FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                BinaryReader br = new BinaryReader(fs);

                Console.WriteLine("Opening ROM..");

                string palDirectory = folder + @"\" + Path.GetFileNameWithoutExtension(fileName);

                if(!Directory.Exists(palDirectory))
                {
                    Directory.CreateDirectory(palDirectory);
                }

                for (int i = 0; i < 56; i++)
                {
                    br.BaseStream.Seek(0x270000 + (i * 6 + 5), SeekOrigin.Begin);
                    byte palIndex = (byte)(br.ReadByte() & 0x7F);

                    br.BaseStream.Seek(0x270150 + (palIndex * 96), SeekOrigin.Begin);
                    byte[] palette16 = br.ReadBytes(96);

                    short[] palArray = new short[48];

                    for(int j = 0; j < 48; j++)
                    {
                        palArray[j] = (short)(palette16[j * 2] + (palette16[j * 2 + 1] << 8));
                    }

                    ushort chunkSize = 4 + 48 * 4;
                    int length = 4 + 4 + 4 + 4 + 4 + 2 + 2 + 48 * 4;
                    byte[] buffer = new byte[length];

                    // the riff header
                    buffer[0] = (byte)'R';
                    buffer[1] = (byte)'I';
                    buffer[2] = (byte)'F';
                    buffer[3] = (byte)'F';
                    PutInt32(length - 8, buffer, 4); // document size

                    // the form type
                    buffer[8] = (byte)'P';
                    buffer[9] = (byte)'A';
                    buffer[10] = (byte)'L';
                    buffer[11] = (byte)' ';
                    
                    // data chunk header
                    buffer[12] = (byte)'d';
                    buffer[13] = (byte)'a';
                    buffer[14] = (byte)'t';
                    buffer[15] = (byte)'a';
                    PutInt32(chunkSize, buffer, 16); // chunk size

                    // logpalette
                    buffer[20] = 0;
                    buffer[21] = 3; // os version (always 03)
                    PutInt16(48, buffer, 22); // colour count


                    for (int j = 0; j < 48; j++)
                    {
                        int offset = 24 + j * 4;
                        byte r = (byte)((palArray[j] % 32) * 8);
                        byte g = (byte)(((palArray[j] / 32) % 32) * 8);
                        byte b = (byte)(((palArray[j] / 1024) % 32) * 8);

                        buffer[offset] = r;
                        buffer[offset + 1] = g;
                        buffer[offset + 2] = b;
                        buffer[offset + 3] = 0;
                    }

                    string palFileName = "BG_" + i.ToString("X2") + "_PAL_" + palIndex.ToString("X2") + ".pal";
                    string fullPath = Path.Combine(palDirectory, palFileName);
                    string relPath = Path.Combine(Path.GetFileName(Path.GetDirectoryName(fullPath)), Path.GetFileName(fullPath));

                    FileStream palfs = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    BinaryWriter stream = new BinaryWriter(palfs);
                    stream.Write(buffer, 0, length);

                    stream.Close();
                    palfs.Close();

                    Console.WriteLine("Creating palette file " + relPath);


                }

                br.Close();
                fs.Close();

                Console.WriteLine(" ");
                Console.WriteLine("Operation completed!");
                Console.ReadKey();
            }
        }

        public static void PutInt32(int value, byte[] buffer, int offset)
        {
            buffer[offset + 3] = (byte)((value & 0xFF000000) >> 24);
            buffer[offset + 2] = (byte)((value & 0x00FF0000) >> 16);
            buffer[offset + 1] = (byte)((value & 0x0000FF00) >> 8);
            buffer[offset] = (byte)((value & 0x000000FF) >> 0);
        }

        public static void PutInt16(ushort value, byte[] buffer, int offset)
        {
            buffer[offset + 1] = (byte)((value & 0x0000FF00) >> 8);
            buffer[offset] = (byte)((value & 0x000000FF) >> 0);
        }
    }
}
