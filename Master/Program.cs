using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace MasterApp
{
    class Program
    {
        static Dictionary<string, Dictionary<string, int>> aggregatedIndex = new();

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: Master.exe <pipeName1> <pipeName2>");
                return;
            }

            string pipe1 = args[0];
            string pipe2 = args[1];

            Console.WriteLine($"Waiting for connection on pipe: {pipe1}");
            Console.WriteLine($"Waiting for connection on pipe: {pipe2}");
            Console.WriteLine("Master is listening to both agents. Press any key to exit...");

            Thread t1 = new Thread(() => HandlePipe(pipe1));
            Thread t2 = new Thread(() => HandlePipe(pipe2));

            t1.Start();
            t2.Start();

            t1.Join();
            t2.Join();

            Console.WriteLine("\nFinal Aggregated Word Index:");
            foreach (var fileEntry in aggregatedIndex)
            {
                foreach (var wordEntry in fileEntry.Value)
                {
                    Console.WriteLine($"{Path.GetFileName(fileEntry.Key)}:{wordEntry.Key}:{wordEntry.Value}");
                }
            }

            Console.ReadKey();
        }

        static void HandlePipe(string pipeName)
        {
            try
            {
                using NamedPipeServerStream pipeServer = new(pipeName, PipeDirection.In);
                pipeServer.WaitForConnection();
                Console.WriteLine($"Connected to {pipeName}");

                using MemoryStream ms = new();
                pipeServer.CopyTo(ms);
                string json = Encoding.UTF8.GetString(ms.ToArray());

                var receivedData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(json);


        
                if (receivedData != null)
                {
                    lock (aggregatedIndex)
                    {
                        foreach (var fileEntry in receivedData)
                        {
                            if (!aggregatedIndex.ContainsKey(fileEntry.Key))
                                aggregatedIndex[fileEntry.Key] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                            foreach (var wordEntry in fileEntry.Value)
                            {
                                if (!aggregatedIndex[fileEntry.Key].ContainsKey(wordEntry.Key))
                                    aggregatedIndex[fileEntry.Key][wordEntry.Key] = 0;

                                aggregatedIndex[fileEntry.Key][wordEntry.Key] += wordEntry.Value;
                            }
                        }
                    }
                }

                Console.WriteLine($"Pipe {pipeName} finished receiving.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling pipe {pipeName}: {ex.Message}");
            }
        }
    }
}

