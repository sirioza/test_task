using Microsoft.Extensions.Options;
using StringsSorter.Extensions;
using StringsSorter.Helpers;
using StringsSorter.Models;
using StringsSorter.Services;
using System.Text;

namespace StringsSorter;

public class Sorter(IMerger merger, IOptions<Options> options)
{
    private readonly string _inputPath = options.Value.InputPath.NotNull(nameof(options.Value.InputPath));
    private readonly string _outputPath = options.Value.OutputPath.NotNull(nameof(options.Value.OutputPath));
    private readonly string _tempDir = options.Value.TempDir.NotNull(nameof(options.Value.TempDir));
    private readonly int _chunkLines = options.Value.ChunkLines.NotZero(nameof(options.Value.ChunkLines));
    private readonly int _bufferSize = options.Value.BufferSize.NotZero(nameof(options.Value.BufferSize));
    private readonly IMerger _merger = merger;

    public List<string> GetOrCreateChunks()
    {
        List<string> chunks = GetChunks();

        if (chunks.Count > 0)
        {
            return chunks;
        }

        int chunkCounter = 0;
        List<string> lines = new(_chunkLines);

        using StreamReader reader = new(_inputPath);

        while (reader.ReadLine() is { } line)
        {
            lines.Add(line);

            if (lines.Count >= _chunkLines)
            {
                SortAndWriteChunkParallel(lines, out string chunkPath, ref chunkCounter);
                chunks.Add(chunkPath);
                lines.Clear();
            }
        }

        if (lines.Count > 0)
        {
            SortAndWriteChunkParallel(lines, out string chunkPath, ref chunkCounter);
            chunks.Add(chunkPath);
        }

        return chunks;
    }

    public async Task MergeSortedChunksAsync(List<string> chunks)
    {
        var readers = new StreamReader[chunks.Count];

        try
        {
            for (int i = 0; i < chunks.Count; i++)
            {
                FileStream fileStream = new(chunks[i], FileMode.Open, FileAccess.Read, FileShare.Read, _bufferSize);
                readers[i] = new StreamReader(fileStream, Encoding.UTF8, false, _bufferSize);
            }

            PriorityQueue<(string line, int idx, string text, long num), (string, long)> priorityQueue = new(
                Comparer<(string, long)>.Create(CompareHelper.CompareLines)
            );

            for (int i = 0; i < chunks.Count; i++)
            {
                string? line = await readers[i].ReadLineAsync();
                if (line == null)
                {
                    continue;
                }

                line.ParseLine(out string? text, out long num);
                priorityQueue.Enqueue((line, i, text, num), (text, num));
            }

            await using FileStream outFileStream = new(
                _outputPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                _bufferSize,
                FileOptions.None);
            await using StreamWriter fileWriter = new(outFileStream, Encoding.UTF8, _bufferSize);

            while (priorityQueue.Count > 0)
            {
                (string line, int idx, _, _) = priorityQueue.Dequeue();
                await fileWriter.WriteLineAsync(line);

                string? nextLine = await readers[idx].ReadLineAsync();
                if (nextLine != null)
                {
                    nextLine.ParseLine(out var nextText, out long nextNum);
                    priorityQueue.Enqueue((nextLine, idx, nextText, nextNum), (nextText, nextNum));
                }
            }
        }
        finally
        {
            foreach (StreamReader streamReader in readers)
            {
                streamReader.Dispose();
            }
        }
    }

    public void RemoveChunks() => Directory.Delete(_tempDir, true);

    private void SortAndWriteChunkParallel(List<string> lines, out string chunkPath, ref int chunkCounter)
    {
        int threadCount = Environment.ProcessorCount;
        int totalLines = lines.Count;

        // parse lines once
        var parsedLines = new LineEntry[totalLines];
        for (int i = 0; i < totalLines; i++)
        {
            lines[i].ParseLine(out var text, out var number);
            parsedLines[i] = new LineEntry { Number = number, Text = text, Original = lines[i] };
        }

        // divide into blocks
        int chunkSize = totalLines / threadCount;
        var blocks = new LineEntry[threadCount][];
        for (int i = 0; i < threadCount; i++)
        {
            int start = i * chunkSize;
            int end = i == threadCount - 1 ? totalLines : start + chunkSize;
            int len = end - start;
            blocks[i] = new LineEntry[len];
            Array.Copy(parsedLines, start, blocks[i], 0, len);
        }

        // sort each block in parallel
        Parallel.For(0, threadCount, blockIdx => { Array.Sort(blocks[blockIdx], CompareHelper.CompareLines); });

        // merge
        chunkPath = Path.Combine(_tempDir, $"chunk_{chunkCounter++}.txt");
        _merger.Merge(blocks, chunkPath);
    }

    private List<string> GetChunks()
    {
        try
        {
            return [.. Directory
            .EnumerateFiles(_tempDir, "chunk_*.txt", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f)];
        }
        catch (DirectoryNotFoundException)
        {
            Directory.CreateDirectory(_tempDir);

            return [];
        }
    }
}