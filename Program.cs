using CommandLine;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace LargeFileSorter
{
    internal class Program
    {
        private class Options
        {
            [Option("File", Default = "", HelpText = "File to sort. If empty file with this name will be generated")]
            public string File { get; set; }

            [Option("LineCount", Default = 1000, HelpText = "Number of line to generate")]
            public int LineCount { get; set; }

            [Option("MaxLength", Default = 50, HelpText = "Max line length")]
            public int LineMaxLength { get; set; }

            [Option("Validate", Default = false, HelpText = "Validate the sorted file")]
            public bool Validate { get; set; }
        }

        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions);
        }

        private static void RunOptions(Options opts)
        {
            Encoding encoding;
            opts.File = Path.GetFullPath(opts.File);

            if (!File.Exists(opts.File))
            {
                Console.WriteLine("Generating new random text file...");
                Console.WriteLine("Number of lines: {0}, Max line length: {1}", opts.LineCount, opts.LineMaxLength);
                opts.File = GenerateRandomFile(opts.File, opts.LineCount, opts.LineMaxLength, out encoding);
                Console.WriteLine("Generated {0}", opts.File);
                Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("Using file {0}", opts.File);
                encoding = GetEncoding(opts.File);
            }

            Console.WriteLine("Sorting file {0}...", opts.File);
            FileSorter largeFileSorter = new FileSorter(encoding);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            string sorted = largeFileSorter.Sort(opts.File);
            stopwatch.Stop();
            Console.WriteLine("Done.");
            Console.WriteLine("Sorted file at: {0}", sorted);
            Console.WriteLine("Sorting took {0}ms", stopwatch.ElapsedMilliseconds);
            Console.WriteLine("");

            if (opts.Validate)
            {
                Console.WriteLine("Validating {0}...", sorted);
                bool valid = SortedFileChecker(sorted);
                Console.WriteLine("Done.");
                if (valid) Console.WriteLine("File is valid!");
                else Console.WriteLine("File is not valid!");
                Console.WriteLine("");
            }
        }

        public static Encoding GetEncoding(string path)
        {
            using (var reader = new StreamReader(path))
            {
                reader.Read();
                return reader.CurrentEncoding;
            }
        }

        public static string GenerateRandomFile(string path, int lineCount, int lineMaxLenght, out Encoding encoding)
        {
            Random rnd = new Random();
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";

            if (string.IsNullOrEmpty(path))
            {
                path = Path.GetFullPath(DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()) + ".txt";
            }

            using (var writer = new StreamWriter(path))
            {
                StringBuilder builder = new StringBuilder(lineMaxLenght);

                for (int i = 0; i < lineCount; i++)
                {
                    int lineLength = rnd.Next(1, lineMaxLenght);
                    for (int j = 0; j < lineLength; j++)
                    {
                        builder.Append(chars[rnd.Next(0, chars.Length)]);
                    }
                    writer.WriteLine(builder.ToString());
                    builder.Clear();
                }

                encoding = writer.Encoding;
            }

            return path;
        }

        public static bool SortedFileChecker(string filename)
        {
            bool valid = true;

            using (var reader = new StreamReader(filename))
            {
                string line1 = reader.ReadLine();
                while (reader.EndOfStream)
                {
                    string line2 = reader.ReadLine();
                    if (string.Compare(line1, line2, StringComparison.Ordinal) < 0)
                    {
                        valid = false;
                        break;
                    }
                    line1 = line2;
                }
            }

            return valid;
        }
    }
}
