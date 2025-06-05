using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;

class AgentA
{
    static void Main(string[] args)
    {
        // Ensure two command-line arguments are provided: pipe name and directory path
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: ScannerA <pipeName> <directoryPath>");
            return;
        }

        string pipeName = args[0];         // Named pipe to communicate with Master
        string directoryPath = args[1];    // Directory to scan for text files

        // Start the file processing in a separate thread
        Thread t = new Thread(() => ProcessFiles(pipeName, directoryPath));
        t.Start();
        t.Join(); // Wait for the processing thread to complete
    }

    static void ProcessFiles(string pipeName, string path)
    {
        // Set this process to use CPU core 0 only (for affinity control)
        Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)(1 << 0); // Core 0

        // Dictionary to hold word count for each file
        Dictionary<string, Dictionary<string, int>> fileWordCounts = new();

        // Iterate over all .txt files in the specified directory
        foreach (var file in Directory.GetFiles(path, "*.txt"))
        {
            // Read the entire file content and split it into lowercase words
            string[] words = File.ReadAllText(file)
                .ToLower()
                .Split(new[] { ' ', '\r', '\n', '\t', '.', ',', ';', ':', '-', '!', '?' }, 
                       StringSplitOptions.RemoveEmptyEntries);

            var wordCount = new Dictionary<string, int>(); // Dictionary to count words in current file

            // Count occurrences of each word
            foreach (var word in words)
            {
                if (!wordCount.ContainsKey(word))
                    wordCount[word] = 0;
                wordCount[word]++;
            }

            // Store the word count dictionary for this file
            fileWordCounts[file] = wordCount;
        }

        // Create a named pipe client and connect to the master process
        using var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
        pipeClient.Connect();

        // Serialize the result (fileWordCounts) to JSON
        var json = JsonSerializer.Serialize(fileWordCounts);

        // Convert the JSON string to bytes and send through the pipe
        var bytes = Encoding.UTF8.GetBytes(json);
        pipeClient.Write(bytes, 0, bytes.Length);
    }
}
