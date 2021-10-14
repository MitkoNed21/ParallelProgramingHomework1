using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;

namespace Homework1
{
    class Program
    {
        static Queue<string> filesToProcess = new Queue<string>();

        static string textFilePath = "";
        static bool processedFile = false;
        static List<string> words = new List<string>();

        static int wordsCount = -1;
        static List<string> shortestWords = new();
        static List<string> longestWords = new();
        static double averageWordLength = -1;
        static List<string> fiveMostCommonWords = new();
        static List<string> fiveLeastCommonWords = new();

        static TimeSpan totalTimeElapsedSynchronously = new TimeSpan(0);
        static TimeSpan totalTimeElapsedInParallel = new TimeSpan(0);


        // Match words like:
        // text, small, голям, по-голям, най-голям

        // Consider numbers with numerals as words
        // 45, 21-ви, 11-ти
        const string WORD_REGEX = @"[a-zA-Zа-яА-Я']+(?:-[a-zA-Zа-яА-Я']+)*|[0-9]+(?:-[a-zA-Zа-яА-Я']+)*";

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            if (args.Length > 0)
            {
                foreach (var filePath in args)
                {
                    if (!File.Exists(filePath))
                    {
                        Console.WriteLine($"File {filePath} was not found!");
                        continue;
                    }

                    filesToProcess.Enqueue(filePath);
                }
            }
            else
            {
                Console.WriteLine("No file was provided. Please provide at least 1 text file to the program.");
                Console.Write("Press any key to exit...");

                Console.ReadKey();
                return;
            }

            if (filesToProcess.Count == 0)
            {
                Console.WriteLine();
                Console.WriteLine("The provided files were not found.");
                Console.Write("Press any key to exit...");

                Console.ReadKey();
                return;
            }

            Console.WriteLine(new String('#', 40));
            Console.WriteLine("Processing files synchronously");
            Console.WriteLine(new String('#', 40));
            Console.WriteLine();
            ProcessFilesSynchronously();

            foreach (var filePath in args)
            {
                filesToProcess.Enqueue(filePath);
            }

            Console.WriteLine(new String('#', 40));
            Console.WriteLine("Processing files in parallel");
            Console.WriteLine(new String('#', 40));
            Console.WriteLine();
            ProcessFilesInParallel();

            Console.WriteLine("Processing time results:");
            Console.WriteLine($"Synchronously: " +
                $"{totalTimeElapsedSynchronously.Minutes:D2}:" +
                $"{totalTimeElapsedSynchronously.Seconds:D2}." +
                $"{totalTimeElapsedSynchronously.Milliseconds:D3}");

            Console.WriteLine($"In parallel: " +
                $"{totalTimeElapsedInParallel.Minutes:D2}:" +
                $"{totalTimeElapsedInParallel.Seconds:D2}." +
                $"{totalTimeElapsedInParallel.Milliseconds:D3}");

            Console.WriteLine();
            Console.Write("Press any key to exit...");
            Console.ReadKey();
        }

        private static void ProcessFilesSynchronously()
        {
            var stopwatch = new Stopwatch();
            
            while (filesToProcess.Count > 0)
            {
                textFilePath = filesToProcess.Dequeue();

                Console.WriteLine($"Loading file {textFilePath} ...");

                words = GetWordsOfTextFromFile();
                if (!processedFile) continue;

                stopwatch.Reset();

                Console.WriteLine($"Processing file...");

                stopwatch.Start();

                int wordsCount = GetWordsNumber();
                List<string> shortestWords = GetShortestWords();
                List<string> longestWords = GetLongestWords();
                double averageWordLength = GetAverageWordLength();
                List<string> fiveMostCommonWords = GetNMostCommonWords(5);
                List<string> fiveLeastCommonWords = GetNLeastCommonWords(5);

                stopwatch.Stop();
                
                PrintResults(wordsCount,
                             shortestWords,
                             longestWords,
                             averageWordLength,
                             fiveMostCommonWords,
                             fiveLeastCommonWords);

                var elapsedTime = stopwatch.Elapsed;

                totalTimeElapsedSynchronously += elapsedTime;

                Console.WriteLine($"Processing took: " +
                    $"{elapsedTime.Minutes:D2}:{elapsedTime.Seconds:D2}.{elapsedTime.Milliseconds:D3}");
                Console.WriteLine(new String('=', 20));
                Console.WriteLine();
            }
        }

        private static void ProcessFilesInParallel()
        {
            var stopwatch = new Stopwatch();

            while (filesToProcess.Count > 0)
            {
                textFilePath = filesToProcess.Dequeue();

                Console.WriteLine($"Loading file {textFilePath} ...");

                words = GetWordsOfTextFromFile();
                if (!processedFile) continue;

                var getWordsNumberThread = new Thread(GetWordsNumberInParallel);
                var getShortestWordsThread = new Thread(GetShortestWordsInParallel);
                var getLongestWordsThread = new Thread(GetLongestWordsInParallel);
                var getAverageWordLengthThread = new Thread(GetAverageWordLengthInParallel);
                var getNMostCommonWordsThread = new Thread(GetNMostCommonWordsInParallel);
                var getNLeastCommonWordsThread = new Thread(GetNLeastCommonWordsInParallel);

                List<Thread> threads = new List<Thread>
                {
                    getWordsNumberThread,
                    getShortestWordsThread,
                    getLongestWordsThread,
                    getAverageWordLengthThread,
                    getNMostCommonWordsThread,
                    getNLeastCommonWordsThread
                };

                stopwatch.Reset();

                Console.WriteLine($"Processing file...");

                stopwatch.Start();

                getWordsNumberThread.Start();
                getShortestWordsThread.Start();
                getLongestWordsThread.Start();
                getAverageWordLengthThread.Start();
                getNMostCommonWordsThread.Start(5);
                getNLeastCommonWordsThread.Start(5);

                while (true)
                {
                    for (int i = 0; i < threads.Count; i++)
                    {
                        if (threads[i].Join(0))
                        {
                            threads.RemoveAt(i);
                            i--;
                        }
                    }

                    if (threads.Count == 0) break;
                }

                stopwatch.Stop();

                PrintResults(wordsCount,
                             shortestWords,
                             longestWords,
                             averageWordLength,
                             fiveMostCommonWords,
                             fiveLeastCommonWords);

                var elapsedTime = stopwatch.Elapsed;

                totalTimeElapsedInParallel += elapsedTime;

                Console.WriteLine($"Processing took: " +
                    $"{elapsedTime.Minutes:D2}:{elapsedTime.Seconds:D2}.{elapsedTime.Milliseconds:D3}");
                Console.WriteLine(new String('=', 20));
                Console.WriteLine();
            }
        }

        private static void GetNLeastCommonWordsInParallel(object obj)
        {
            var n = (int)obj;
            fiveLeastCommonWords = GetNLeastCommonWords(n);
        }

        private static void GetNMostCommonWordsInParallel(object obj)
        {
            var n = (int)obj;
            fiveMostCommonWords = GetNMostCommonWords(n);
        }

        private static void GetAverageWordLengthInParallel()
        {
            averageWordLength = GetAverageWordLength();
        }

        private static void GetLongestWordsInParallel()
        {
            longestWords = GetLongestWords();
        }

        private static void GetShortestWordsInParallel()
        {
            shortestWords = GetShortestWords();
        }

        private static void GetWordsNumberInParallel()
        {
            wordsCount = GetWordsNumber();
        }

        private static List<string> GetNLeastCommonWords(int n)
        {
            var wordsWithCount = new Dictionary<string, int>();

            foreach (var word in words)
            {
                if (!wordsWithCount.ContainsKey(word))
                {
                    wordsWithCount.Add(word, 1);
                }
                else
                {
                    wordsWithCount[word]++;
                }
            }

            return wordsWithCount
                .OrderBy(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .Take(n)
                .ToList();
        }

        private static List<string> GetNMostCommonWords(int n)
        {
            var wordsWithCount = new Dictionary<string, int>();

            foreach (var word in words)
            {
                if (!wordsWithCount.ContainsKey(word))
                {
                    wordsWithCount.Add(word, 1);
                }
                else
                {
                    wordsWithCount[word]++;
                }
            }

            return wordsWithCount
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .Take(n)
                .ToList();
        }

        private static double GetAverageWordLength()
        {
            double length = 0;
            foreach (var word in words)
            {
                length += word.Length;
            }

            return length /= words.Count;
        }

        private static List<string> GetLongestWords()
        {
            var result = new List<string>();
            var maxWordLength = -1;

            foreach (var word in words)
            {
                if (word.Length > maxWordLength)
                {
                    maxWordLength = word.Length;
                }
            }

            foreach (var word in words)
            {
                if (word.Length == maxWordLength && !result.Contains(word))
                {
                    result.Add(word);
                }
            }

            return result;
        }

        private static List<string> GetShortestWords()
        {
            var result = new List<string>();
            var minWordLength = int.MaxValue;

            foreach (var word in words)
            {
                if (word.Length < minWordLength)
                {
                    minWordLength = word.Length;
                }
            }

            foreach (var word in words)
            {
                if (word.Length == minWordLength && !result.Contains(word))
                {
                    result.Add(word);
                }
            }

            return result;
        }

        private static int GetWordsNumber()
        {
            // intentionally counting words in this way
            var count = 0;
            using var enumerator = words.GetEnumerator();

            while (enumerator.MoveNext())
            {
                count++;
            }

            return count;
        }

        private static List<string> GetWordsOfTextFromFile()
        {
            var result = new List<string>();
            var text = "";

            processedFile = false;

            try
            {
                text = File.ReadAllText(textFilePath, Encoding.UTF8).ToLower();
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not read the file!");
                Console.WriteLine(e.Message);
                return null;
            }

            var foundWords = Regex.Matches(text, WORD_REGEX, RegexOptions.None, Regex.InfiniteMatchTimeout);

            foreach (Match match in foundWords)
            {
                result.Add(match.Value);
            }

            processedFile = true;
            return result;
        }

        private static void PrintResults(int wordsCount, List<string> shortestWords, List<string> longestWords, double averageWordLength, List<string> fiveMostCommonWords, List<string> fiveLeastCommonWords)
        {
            Console.WriteLine(new String('=', 20));
            Console.WriteLine($"File: {textFilePath}");
            Console.WriteLine(new String('-', 20));
            Console.WriteLine($"Words: {wordsCount}");
            Console.WriteLine($"Shortest word(s) ({shortestWords.First().Length} characters): {String.Join(", ", shortestWords)}");
            Console.WriteLine($"Longest word(s) ({longestWords.First().Length} characters): {String.Join(", ", longestWords)}");
            Console.WriteLine($"Average word length: {averageWordLength:F5} characters");
            Console.WriteLine($"Five most common words: {String.Join(", ", fiveMostCommonWords)}");
            Console.WriteLine($"Five least common words: {String.Join(", ", fiveLeastCommonWords)}");
            Console.WriteLine(new String('=', 20));
        }
    }
}
