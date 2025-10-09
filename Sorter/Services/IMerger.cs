using StringsSorter.Models;

namespace StringsSorter.Services;

public interface IMerger
{
    void Merge(LineEntry[][] blocks, string outputPath);
}
