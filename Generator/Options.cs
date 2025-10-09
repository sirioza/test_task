namespace StringsGenerator;

public class Options
{
    public required string OutputPath { get; init; }
    public required long TargetSizeBytes { get; init; }
    public required byte Workers { get; init; }
    public required int BufferSize { get; init; }
}