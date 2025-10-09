namespace StringsSorter;

public class Options
{
    public required string InputPath { get; init; }
    public required string OutputPath { get; init; }
    public required string TempDir { get; init; }
    public required int ChunkLines { get; init; }
    public required int BufferSize { get; init; }
}
