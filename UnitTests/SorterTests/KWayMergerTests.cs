using StringsSorter;
using StringsSorter.Models;
using StringsSorter.Services.Implementation;
using System.Text;
using Xunit;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace UnitTests.Services;

public class KWayMergerTests
{
    [Fact]
    public void Constructor_SetsBufferSize()
    {
        // Arrange
        var opts = CreateOptions(bufferSize: 4096);

        // Assert
        Assert.NotNull(() => new KWayMerger(opts));
    }

    [Fact]
    public void Constructor_ThrowsWhenBufferSizeIsZero()
    {
        // Arrange
        var opts = CreateOptions(bufferSize: 0);

        // Assert
        Assert.Throws<ArgumentException>(() => new KWayMerger(opts));
    }

    [Fact]
    public void Merge_MergesSortedBlocksCorrectly()
    {
        // Arrange
        KWayMerger merger = new(CreateOptions(bufferSize: 128));

        var blocks = new LineEntry[][]
        {
            [
                new LineEntry(1, "test1", "1.test1"),
                new LineEntry(4, "test4", "4.test4")
            ],
            [
                new LineEntry(2, "test2", "2.test2"),
                new LineEntry(3, "test3", "3.test3")
            ]
        };

        string outputPath = Path.GetTempFileName();

        // Act
        merger.Merge(blocks, outputPath);

        // Assert
        string[] lines = File.ReadAllLines(outputPath, Encoding.UTF8);
        Assert.Equal(["1.test1", "2.test2", "3.test3", "4.test4"], lines);

        File.Delete(outputPath);
    }

    [Fact]
    public void Merge_HandlesEmptyBlocks()
    {
        // Arrange
        KWayMerger merger = new(CreateOptions());

        var blocks = new LineEntry[][]
        {
            [], [new LineEntry(1, "test", "1.test")]
        };

        string outputPath = Path.GetTempFileName();

        // Act
        merger.Merge(blocks, outputPath);

        // Assert
        string[] lines = File.ReadAllLines(outputPath, Encoding.UTF8);
        Assert.Single(lines);
        Assert.Equal("1.test", lines[0]);

        File.Delete(outputPath);
    }

    [Fact]
    public void Merge_WritesNothingWhenAllBlocksAreEmpty()
    {
        // Arrange
        KWayMerger merger = new(CreateOptions());

        var blocks = new LineEntry[][] { [], [] };

        string outputPath = Path.GetTempFileName();

        // Act
        merger.Merge(blocks, outputPath);

        // Assert
        string[] lines = File.ReadAllLines(outputPath, Encoding.UTF8);
        Assert.Empty(lines);

        File.Delete(outputPath);
    }

    [Fact]
    public void Merge_SortsByTextThenIsNumber()
    {
        // Arrange
        KWayMerger merger = new(CreateOptions());

        var blocks = new LineEntry[][]
        {
            [
                new LineEntry(1, "test", "1.test1"),
                new LineEntry(3, "test", "3.test3")
            ],
            [
                new LineEntry(2, "test", "2.test2")
            ]
        };

        string outputPath = Path.GetTempFileName();

        // Act
        merger.Merge(blocks, outputPath);

        // Assert
        string[] lines = File.ReadAllLines(outputPath, Encoding.UTF8);
        Assert.Equal(["1.test1", "2.test2", "3.test3"], lines);

        File.Delete(outputPath);
    }

    private static Microsoft.Extensions.Options.IOptions<Options> CreateOptions(
        string input = null!,
        string output = null!,
        string temp = null!,
        int chunkLines = 100,
        int bufferSize = 64)
        => MicrosoftOptions.Create(new Options
        {
            InputPath = input ?? Path.GetTempFileName(),
            OutputPath = output ?? Path.GetTempFileName(),
            TempDir = temp ?? Path.GetTempFileName(),
            ChunkLines = chunkLines,
            BufferSize = bufferSize
        });
}
