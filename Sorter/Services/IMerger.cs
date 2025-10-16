using StringsSorter.Models;

namespace StringsSorter.Services;

public interface IMerger
{
    void Merge(Memory<LineEntry>[] blocks, string outputPath);
}
