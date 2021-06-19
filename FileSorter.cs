using System;
using System.IO;
using System.Linq;
using System.Text;

namespace LargeFileSorter
{
    internal class FileSorter
    {
        private FileStream source;
        private FileStream map;
        private readonly Encoding encoding;

        public FileSorter(Encoding encoding)
        {
            this.encoding = encoding;
        }

        // Buffers are declared as fields to avoid allocating them in stack at every method call.
        private byte[] strbuff;
        private readonly byte[] buff1 = new byte[12];
        private readonly byte[] buff2 = new byte[12];

        /// <summary>
        /// Sorts a large text file.
        /// </summary>
        /// <param name="path">Path to unsorted text file</param>
        /// <returns>Path to sorted file.</returns>
        public string Sort(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException($"'{nameof(path)}' cannot be null or whitespace.", nameof(path));
            }

            string fullpath = Path.GetFullPath(path);
            string mapPath = fullpath + "_map.bin";
            string outputfile;

            try
            {
                source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
                map = new FileStream(mapPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                Console.WriteLine("-- Source file is {0} bytes", source.Length);

                // Step 1
                // Building a binary map of the source text file.
                // The map contains a Position and Length of every line in source file.
                // Each record in map file is 12 bytes long: int (4bytes) + long (8bytes).
                Console.WriteLine("-- Building file map...");
                int maxLength = BuildMap();
                // The buffer is created to hold a line in binary form.
                // It is set to fit the longest line.
                // This way the program memory usage depends on LineMaxLength parameter.
                strbuff = new byte[maxLength];

                // Step 2
                // Sorting map file.
                // The values for caparison a read from the source file during sorting.
                // As a  result map file is sorted. (Index-based sort).
                // Comb sort is the sorting algorithm.
                Console.WriteLine("-- Sorting file map...");
                MapSort();

                // Step 3
                // Generate the sorted text file.
                Console.WriteLine("-- Writing sorted file...");
                outputfile = Write();
            }
            finally
            {
                source.Dispose();
                map.Dispose();
            }

            // Deletes the map file.
            File.Delete(map.Name);

            return outputfile;
        }

        private int BuildMap()
        {
            byte[] NL = encoding.GetBytes(Environment.NewLine);

            // Detect BOM at the start of a file
            byte[] BOM = new byte[encoding.GetPreamble().Length];
            int read = source.Read(BOM);
            if (read != BOM.Length || !BOM.SequenceEqual(encoding.GetPreamble()))
            {
                // File does not have BOM at the beginning
                source.Seek(0, SeekOrigin.Begin);
            }
            // else: Position already is set after the BOM

            int Pos = 0;
            int LineLength = 0;
            int MaxLength = 0;
            long LineStartPosition = source.Position; // In case of BOM will be set to 3, otherwise to 0;

            while (true)
            {
                int b = source.ReadByte();
                if (b == -1) break;

                LineLength++;
                if (NL[Pos] == (byte)b)
                {
                    Pos++;

                    // Check if we found a NewLine
                    if (Pos == NL.Length)
                    {
                        LineLength -= NL.Length;
                        // Write line information to map file
                        map.Write(BitConverter.GetBytes(LineStartPosition));
                        map.Write(BitConverter.GetBytes(LineLength));

                        // Get maximum string length
                        if (LineLength > MaxLength) MaxLength = LineLength;

                        LineStartPosition = source.Position;
                        LineLength = 0;
                        Pos = 0;
                    }
                }
                else
                {
                    Pos = 0;
                }

            }

            // Check if file ends without a NewLine
            if (LineLength > 0)
            {
                map.Write(BitConverter.GetBytes(LineStartPosition));
                map.Write(BitConverter.GetBytes(LineLength));
                // The last line might be the longest
                if (LineLength > MaxLength) MaxLength = LineLength;
            }

            map.Flush();
            return MaxLength;
        }
        private void MapSort()
        {
            int Start = 0;                      // The algorithm sorts from the first element
            int Stop = (int)(map.Length / 12);  // to the last element

            int Shift; bool Sw;
            int Bound;

            Shift = (int)((Stop - Start) / 1.247f);
            if (Shift <= 0) { Shift = 1; }


            while (true)
            {
                Sw = false;
                Bound = Stop - Shift;

                for (int i = Start; i <= Bound; i++)
                {
                    if (Compare(i, i + Shift) > 0)
                    {
                        Swap(i, i + Shift);
                        Sw = true;
                    }
                }

                if (Sw == false && Shift == 1) { break; }
                if (Shift > 1) { Shift = (int)(Shift / 1.247f); }
            }
        }
        private string Write()
        {
            string filename = source.Name + "_sorted.txt";
            StreamWriter writer = new StreamWriter(filename, false, encoding);
            map.Seek(0, SeekOrigin.Begin);

            int n = (int)(map.Length / 12);
            for (int i = 0; i < n; i++)
            {
                map.Read(buff1, 0, 12);
                long position = BitConverter.ToInt64(buff1, 0);
                int strLength = BitConverter.ToInt32(buff1, 8);

                source.Seek(position, SeekOrigin.Begin);
                source.Read(strbuff, 0, strLength);
                string line = encoding.GetString(strbuff, 0, strLength);

                writer.WriteLine(line);
            }

            writer.Flush();
            writer.Dispose();

            return filename;
        }
        private int Compare(int pos1, int pos2)
        {
            // Read string from position 1
            map.Seek(pos1 * 12, SeekOrigin.Begin);
            map.Read(buff1, 0, 12);
            long position = BitConverter.ToInt64(buff1, 0);
            int strLength = BitConverter.ToInt32(buff1, 8);

            source.Seek(position, SeekOrigin.Begin);
            source.Read(strbuff, 0, strLength);

            string S1 = encoding.GetString(strbuff, 0, strLength);

            // Read string from position 2
            map.Seek(pos2 * 12, SeekOrigin.Begin);
            map.Read(buff1, 0, 12);
            position = BitConverter.ToInt64(buff1, 0);
            strLength = BitConverter.ToInt32(buff1, 8);

            source.Seek(position, SeekOrigin.Begin);
            source.Read(strbuff, 0, strLength);

            string S2 = encoding.GetString(strbuff, 0, strLength);

            return string.Compare(S1, S2, StringComparison.Ordinal);
        }
        private void Swap(int pos1, int pos2)
        {
            long P1 = pos1 * 12, P2 = pos2 * 12;

            map.Seek(P1, SeekOrigin.Begin);
            map.Read(buff1, 0, 12);
            map.Seek(P2, SeekOrigin.Begin);
            map.Read(buff2, 0, 12);

            map.Seek(P1, SeekOrigin.Begin);
            map.Write(buff2, 0, 12);
            map.Seek(P2, SeekOrigin.Begin);
            map.Write(buff1, 0, 12);
        }
    }
}
