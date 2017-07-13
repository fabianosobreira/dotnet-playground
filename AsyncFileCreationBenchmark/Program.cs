using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AsyncFileCreationBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            const int filesToCreate = 1000;
            const int fileSizeInBytes = 1024 * 10;

            IEnumerable<string> fileNames;

            FileCreator fileCreator = new FileCreator();

            fileNames = GenerateFileNames(filesToCreate);
            fileCreator.BechmarkSyncronousFileCreation(fileNames, fileSizeInBytes);
            fileCreator.Cleanup(fileNames);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            fileNames = GenerateFileNames(filesToCreate);
            fileCreator.BenchmarkAsyncronousFileCreationInAForeachLoop(fileNames, fileSizeInBytes).Wait();
            fileCreator.Cleanup(fileNames);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            fileNames = GenerateFileNames(filesToCreate);
            fileCreator.BenchmarkAsyncronousFileCreationWithTaskWhenAll(fileNames, fileSizeInBytes).Wait();
            fileCreator.Cleanup(fileNames);

            //Console.ReadKey();
        }

        private static IEnumerable<string> GenerateFileNames(int filesToCreate)
        {
            List<string> fileNames = new List<string>();

            for (int i = 0; i < filesToCreate; i++)
            {
                fileNames.Add($"{Environment.GetEnvironmentVariable("TEMP")}\\{Guid.NewGuid().ToString("N")}.tmp");
            }

            return fileNames;
        }

        class FileCreator
        {
            public void Cleanup(IEnumerable<string> fileNames)
            {
                foreach (var fileName in fileNames)
                {
                    System.IO.File.Delete(fileName);
                }
            }

            public void BechmarkSyncronousFileCreation(
                IEnumerable<string> fileNames,
                int fileSizeInBytes)
            {
                byte[] fileBuffer = new byte[fileSizeInBytes];

                Random random = new Random();
                random.NextBytes(fileBuffer);

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                foreach (var fileName in fileNames)
                {
                    CreateFile(fileBuffer, fileName);
                }

                stopWatch.Stop();
                Console.WriteLine($"BechmarkSyncronousFileCreation finished in {stopWatch.ElapsedMilliseconds} milliseconds.");
            }

            public async Task BenchmarkAsyncronousFileCreationInAForeachLoop(
                IEnumerable<string> fileNames,
                int fileSizeInBytes)
            {
                byte[] fileBuffer = new byte[fileSizeInBytes];

                Random random = new Random();
                random.NextBytes(fileBuffer);

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                foreach (var fileName in fileNames)
                {
                    await CreateFileAsync(fileBuffer, fileName).ConfigureAwait(false);
                }

                stopWatch.Stop();
                Console.WriteLine($"BenchmarkAsyncronousFileCreationInAForeachLoop finished in {stopWatch.ElapsedMilliseconds} milliseconds.");
            }

            public async Task BenchmarkAsyncronousFileCreationWithTaskWhenAll(
                IEnumerable<string> fileNames,
                int fileSizeInBytes)
            {
                byte[] fileBuffer = new byte[fileSizeInBytes];

                Random random = new Random();
                random.NextBytes(fileBuffer);

                List<Task> tasks = new List<Task>();

                foreach (var fileName in fileNames)
                {
                    tasks.Add(CreateFileAsync(fileBuffer, fileName));
                }

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                await Task.WhenAll(tasks).ConfigureAwait(false);

                stopWatch.Stop();
                Console.WriteLine($"BenchmarkAsyncronousFileCreationWithTaskWhenAll in {stopWatch.ElapsedMilliseconds} milliseconds.");
            }

            private void CreateFile(byte[] fileBuffer, string fileName)
            {
                using (var file = System.IO.File.Create(fileName))
                {
                    file.Write(fileBuffer, 0, fileBuffer.Length);
                    file.Flush();
                }
            }

            private async Task CreateFileAsync(byte[] fileBuffer, string fileName)
            {
                using (var file = System.IO.File.Create(fileName))
                {
                    await file.WriteAsync(fileBuffer, 0, fileBuffer.Length);
                    await file.FlushAsync();
                }
            }

        }
    }
}
