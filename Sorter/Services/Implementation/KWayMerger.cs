using Microsoft.Extensions.Options;
using StringsSorter.Extensions;
using StringsSorter.Helpers;
using StringsSorter.Models;
using System.Text;

namespace StringsSorter.Services.Implementation;

public class KWayMerger(IOptions<Options> options) : IMerger
{
    private readonly int _bufferSize = options.Value.BufferSize.NotZero(nameof(options.Value.BufferSize));

    public void Merge(LineEntry[][] blocks, string outputPath)
    {
        PriorityQueue<(LineEntry entry, int blockIdx, int idxInBlock), (string, long)> priorityQueue =
            new(Comparer<(string, long)>.Create(CompareHelper.CompareLines));

        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i].Length > 0)
            {
                priorityQueue.Enqueue((blocks[i][0], i, 0), (blocks[i][0].Text, blocks[i][0].Number));
            }
        }

        using StreamWriter fileWriter = new(outputPath, false, Encoding.UTF8, _bufferSize);

        while (priorityQueue.Count > 0)
        {
            (LineEntry entry, int blockIdx, int idxInBlock) = priorityQueue.Dequeue();
            fileWriter.WriteLine(entry.Original);

            idxInBlock++;
            if (idxInBlock < blocks[blockIdx].Length)
            {
                LineEntry next = blocks[blockIdx][idxInBlock];
                priorityQueue.Enqueue((next, blockIdx, idxInBlock), (next.Text, next.Number));
            }
        }
    }
}
