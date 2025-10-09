using StringsGenerator;
using StringsGenerator.Workers;
using System.Buffers;
using System.Text;
using System.Threading.Channels;
using Xunit;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace UnitTests.Workers;

public class ConsumerTests
{
    [Fact]
    public async Task RunAsync_WritesDataToFile()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<(byte[] Buffer, int Count)>();
        var options = CreateOptions(Path.GetTempFileName());
        Consumer consumer = new(channel, options);

        var buffer = ArrayPool<byte>.Shared.Rent(5); // 5 bytes for the "test\n"
        Encoding.UTF8.GetBytes("test\n", 0, 5, buffer, 0);
        await channel.Writer.WriteAsync((buffer, 5));
        channel.Writer.Complete();

        // Act
        await consumer.RunAsync();

        // Assert
        string content = File.ReadAllText(options.Value.OutputPath);
        Assert.Contains("test", content);

        File.Delete(options.Value.OutputPath);
    }

    [Fact]
    public async Task RunAsync_RespectsTargetSizeBytes()
    {
        // Arrange
        var options = CreateOptions(Path.GetTempFileName(), targetSizeBytes: 3);
        var channel = Channel.CreateUnbounded<(byte[] Buffer, int Count)>();
        Consumer consumer = new(channel, options);

        var buffer = ArrayPool<byte>.Shared.Rent(4);
        Encoding.UTF8.GetBytes("test", 0, 4, buffer, 0);
        await channel.Writer.WriteAsync((buffer, 4));
        channel.Writer.Complete();

        // Act
        await consumer.RunAsync();

        // Assert
        string content = File.ReadAllText(options.Value.OutputPath);
        Assert.Equal(3, content.Length);

        File.Delete(options.Value.OutputPath);
    }

    [Fact]
    public async Task RunAsync_ReturnsBuffersToArrayPool()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<(byte[] Buffer, int Count)>();
        var options = CreateOptions(Path.GetTempFileName());
        Consumer consumer = new(channel, options);

        var buffer = ArrayPool<byte>.Shared.Rent(10);
        Encoding.UTF8.GetBytes("test\n", buffer);
        await channel.Writer.WriteAsync((buffer, 4));
        channel.Writer.Complete();

        // Act
        await consumer.RunAsync();

        // Assert
        File.Delete(options.Value.OutputPath);
    }

    [Fact]
    public void Constructor_ThrowsWhenOutputPathIsNullOrEmpty()
    {
        // Arrange
        var options = CreateOptions(null!);

        // Assert
        Assert.Throws<ArgumentException>(() => new Consumer(null!, options));
    }

    private static Microsoft.Extensions.Options.IOptions<Options> CreateOptions(
        string output,
        byte workers = 2,
        int targetSizeBytes = 1 * 1024,
        int bufferSize = 64)
        => MicrosoftOptions.Create(new Options
        {
            Workers = workers,
            BufferSize = bufferSize,
            OutputPath = output,
            TargetSizeBytes = targetSizeBytes
        });
}
