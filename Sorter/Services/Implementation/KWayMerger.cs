using Microsoft.Extensions.Options;
using StringsSorter.Extensions;
using StringsSorter.Helpers;
using StringsSorter.Models;
using System.Text;

namespace StringsSorter.Services.Implementation;

public class KWayMerger(IOptions<Options> options) : IMerger
{
    private readonly int _bufferSize = options.Value.BufferSize.NotZero(nameof(options.Value.BufferSize));

    public void Merge(Memory<LineEntry>[] blocks, string outputPath)
    {
        PriorityQueue<(LineEntry entry, int blockIdx, int idxInBlock), (string, long)> priorityQueue =
            new(Comparer<(string, long)>.Create(CompareHelper.CompareLines));

        for (int i = 0; i < blocks.Length; i++)
        {
            Span<LineEntry> block = blocks[i].Span;
            if (!block.IsEmpty)
            {
                priorityQueue.Enqueue((block[0], i, 0), (block[0].Text, block[0].Number));
            }
        }

        using StreamWriter fileWriter = new(outputPath, false, Encoding.UTF8, _bufferSize);

        while (priorityQueue.Count > 0)
        {
            (LineEntry entry, int blockIdx, int idxInBlock) = priorityQueue.Dequeue();
            fileWriter.WriteLine(entry.Original);

            idxInBlock++;
            Span<LineEntry> block = blocks[blockIdx].Span;
            if (idxInBlock < block.Length)
            {
                LineEntry next = block[idxInBlock];
                priorityQueue.Enqueue((next, blockIdx, idxInBlock), (next.Text, next.Number));
            }
        }
    }
}
