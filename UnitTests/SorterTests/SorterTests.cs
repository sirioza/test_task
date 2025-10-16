using NSubstitute;
using StringsSorter;
using StringsSorter.Models;
using StringsSorter.Services;
using System.Text;
using Xunit;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace UnitTests.Services;

public class SorterTests
{

    [Fact]
    public void Constructor_ThrowsWhenOptionsAreInvalid()
    {
        // Arrange
        string path = Path.GetTempFileName();
        var invalidCases = new[]
        {
            new Options { InputPath = null!, OutputPath = path, TempDir = path, ChunkLines = 1, BufferSize = 1 },
            new Options { InputPath = path, OutputPath = null!, TempDir = path, ChunkLines = 1, BufferSize = 1 },
            new Options { InputPath = path, OutputPath = path, TempDir = null!, ChunkLines = 1, BufferSize = 1 },
            new Options { InputPath = path, OutputPath = path, TempDir = path, ChunkLines = 0, BufferSize = 1 },
            new Options { InputPath = path, OutputPath = path, TempDir = path, ChunkLines = 1, BufferSize = 0 },
        };

        foreach (Options opt in invalidCases)
        {
            var options = MicrosoftOptions.Create(opt);
            // Act
            Assert.Throws<ArgumentException>(() => new Sorter(null!, options));
        }
    }


    [Fact]
    public void Constructor_SetsFieldsWhenAreValid()
    {
        // Arrange
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(directory);
        var option = CreateOptions(Path.GetTempFileName(), Path.GetTempFileName(), directory);
        var merger = Substitute.For<IMerger>();

        // Act
        Sorter sorter = new(merger, option);

        // Assert
        Assert.NotNull(sorter);

        Directory.Delete(directory, true);
        File.Delete(option.Value.InputPath);
        File.Delete(option.Value.OutputPath);
    }

    [Fact]
    public void GetChunks_CreatesDirectoryWhenDoesntExist()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var option = CreateOptions(Path.GetTempFileName(), Path.GetTempFileName(), tempDir);
        var merger = Substitute.For<IMerger>();
        Sorter sorter = new(merger, option);

        // Act
        List<string> chunks = sorter.GetOrCreateChunks();

        // Assert
        Assert.Empty(chunks);
        Assert.True(Directory.Exists(tempDir));

        // Cleanup
        Directory.Delete(tempDir, true);
        File.Delete(option.Value.InputPath);
        File.Delete(option.Value.OutputPath);
    }

    [Fact]
    public void GetOrCreateChunks_CreatesChunksWhenInputHasData()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var option = CreateOptions(Path.GetTempFileName(), Path.GetTempFileName(), tempDir);

        File.WriteAllLines(option.Value.InputPath, ["1.test1", "2.test2", "3.test3"], Encoding.UTF8);

        var merger = Substitute.For<IMerger>();
        merger
            .When(m => m.Merge(Arg.Any<Memory<LineEntry>[]>(), Arg.Any<string>()))
            .Do(callInfo =>
            {
                var blocks = callInfo.Arg<Memory<LineEntry>[]>();
                var path = callInfo.Arg<string>();
                File.WriteAllLines(path, blocks.SelectMany(b => b.Span.ToArray()).Select(x => x.Original));
            });

        Sorter sorter = new(merger, CreateOptions(option.Value.InputPath, option.Value.OutputPath, tempDir, 2));

        // Act
        List<string> chunks = sorter.GetOrCreateChunks();

        // Assert
        Assert.NotEmpty(chunks);
        Assert.All(chunks, chunk => Assert.True(File.Exists(chunk)));

        Directory.Delete(tempDir, true);
        File.Delete(option.Value.InputPath);
        File.Delete(option.Value.OutputPath);
    }

    [Fact]
    public async Task MergeSortedChunksAsync_MergesСorrectly()
    {
        // Arrange
        string[] strings = ["1.test1", "2.test2", "3.test3", "4.test4"];
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var option = CreateOptions(Path.GetTempFileName(), Path.Combine(tempDir, Path.GetTempFileName()), tempDir);
        string chunk1 = Path.Combine(tempDir, "chunk1.txt");
        string chunk2 = Path.Combine(tempDir, "chunk2.txt");

        await File.WriteAllLinesAsync(chunk1, [strings[0], strings[2]]);
        await File.WriteAllLinesAsync(chunk2, [strings[1], strings[3]]);

        var merger = Substitute.For<IMerger>();

        Sorter sorter = new(merger, CreateOptions(option.Value.InputPath, option.Value.OutputPath, tempDir, 2));

        // Act
        await sorter.MergeSortedChunksAsync([chunk1, chunk2]);

        // Assert
        string[] lines = File.ReadAllLines(option.Value.OutputPath, Encoding.UTF8);
        Assert.Equal(strings, lines);

        Directory.Delete(tempDir, true);
        File.Delete(option.Value.InputPath);
    }

    [Fact]
    public void RemoveChunks_DeletesDirectory()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var merger = Substitute.For<IMerger>();
        Sorter sorter = new(merger, CreateOptions(Path.GetTempFileName(), Path.GetTempFileName(), tempDir));

        // Act
        sorter.RemoveChunks();

        // Assert
        Assert.False(Directory.Exists(tempDir));
    }

    private static Microsoft.Extensions.Options.IOptions<Options> CreateOptions(
        string input,
        string output,
        string temp,
        int chunkLines = 2,
        int bufferSize = 128)
        => MicrosoftOptions.Create(new Options
        {
            InputPath = input,
            OutputPath = output,
            TempDir = temp,
            ChunkLines = chunkLines,
            BufferSize = bufferSize
        });
}
