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
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: ScannerA <pipeName> <directoryPath>");
            return;
        }

        string pipeName = args[0];
        string directoryPath = args[1];

        Thread t = new Thread(() => ProcessFiles(pipeName, directoryPath));
        t.Start();
        t.Join();
    }

    static void ProcessFiles(string pipeName, string path)
    {
        Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)(1 << 0); // Core 0

        Dictionary<string, Dictionary<string, int>> fileWordCounts = new();

        foreach (var file in Directory.GetFiles(path, "*.txt"))
        {
            string[] words = File.ReadAllText(file)
                .ToLower()
                .Split(new[] { ' ', '\r', '\n', '\t', '.', ',', ';', ':', '-', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

            var wordCount = new Dictionary<string, int>();
            foreach (var word in words)
            {
                if (!wordCount.ContainsKey(word))
                    wordCount[word] = 0;
                wordCount[word]++;
            }

            fileWordCounts[file] = wordCount;
        }

        using var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
        pipeClient.Connect();

        var json = JsonSerializer.Serialize(fileWordCounts);
        var bytes = Encoding.UTF8.GetBytes(json);
        pipeClient.Write(bytes, 0, bytes.Length);
    }
}
