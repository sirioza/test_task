using NSubstitute;
using StringsGenerator;
using StringsGenerator.Workers.Base;
using System.Text;
using System.Threading.Channels;
using Xunit;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace UnitTests.GeneratorTests;

public class GeneratorTests
{
    [Fact]
    public void Constructor_ThrowsWhenWorkersIsZero()
    {
        // Arrange
        var options = MicrosoftOptions.Create(new Options
        {
            Workers = 0,
            BufferSize = 0,
            OutputPath = null!,
            TargetSizeBytes = 0
        });

        // Assert
        Assert.Throws<ArgumentException>(() => new Generator(null!, null!, null!, options));
    }

    [Fact]
    public async Task GenerateAsync_ProducesDataThroughChannel()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<(byte[], int)>();
        var producer = Substitute.For<IChannelWorker>();
        var consumer = Substitute.For<IChannelWorker>();
        var options = CreateOptions(1);

        producer
            .RunAsync()
            .Returns(async _ =>
            {
                var buffer = Encoding.UTF8.GetBytes("test\n");
                await channel.Writer.WriteAsync((buffer, buffer.Length));
            });

        consumer
            .RunAsync()
            .Returns(async _ =>
            {
                await foreach (var (buf, count) in channel.Reader.ReadAllAsync())
                {
                    Assert.Equal("test\n", Encoding.UTF8.GetString(buf, 0, count));
                }
            });

        var generator = new Generator(channel, consumer, producer, options);

        // Act
        await generator.GenerateAsync();

        // Assert
        await producer.Received(options.Value.Workers).RunAsync();
        await consumer.Received(1).RunAsync();
        Assert.True(channel.Reader.Completion.IsCompleted);
    }



    [Fact]
    public async Task GenerateAsync_ClosesChannelAfterProducers()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<(byte[], int)>();
        var producer = Substitute.For<IChannelWorker>();
        var consumer = Substitute.For<IChannelWorker>();

        producer.RunAsync().Returns(Task.CompletedTask);
        consumer.RunAsync().Returns(Task.CompletedTask);

        var options = CreateOptions();
        var generator = new Generator(channel, consumer, producer, options);

        // Act
        await generator.GenerateAsync();

        // Assert
        Assert.True(channel.Reader.Completion.IsCompleted);
    }



    [Fact]
    public async Task GenerateAsync_WaitsForAllProducers()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<(byte[], int)>();
        var producer = Substitute.For<IChannelWorker>();
        var consumer = Substitute.For<IChannelWorker>();

        var tcs1 = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();
        int callCount = 0;

        producer
            .RunAsync()
            .Returns(_ =>
            {
                callCount++;
                return callCount == 1 ? tcs1.Task : tcs2.Task;
            });

        consumer.RunAsync().Returns(Task.CompletedTask);

        var options = CreateOptions(2);
        var generator = new Generator(channel, consumer, producer, options);

        // Act
        Task task = generator.GenerateAsync();

        // Assert
        Assert.False(task.IsCompleted);

        tcs1.SetResult();
        Assert.False(task.IsCompleted);

        tcs2.SetResult();
        await task;
    }


    [Fact]
    public async Task GenerateAsync_CallsConsumerOnce()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<(byte[], int)>();
        var producer = Substitute.For<IChannelWorker>();
        var consumer = Substitute.For<IChannelWorker>();

        producer.RunAsync().Returns(Task.CompletedTask);
        consumer.RunAsync().Returns(Task.CompletedTask);

        var options = CreateOptions();
        var generator = new Generator(channel, consumer, producer, options);

        // Act
        await generator.GenerateAsync();

        // Assert
        await consumer.Received(1).RunAsync();
        await producer.Received(options.Value.Workers).RunAsync();
    }

    private static Microsoft.Extensions.Options.IOptions<Options> CreateOptions(
        byte workers = 2,
        string output = null!,
        int targetSizeBytes = 1 * 1024 * 1024,
        int bufferSize = 4096)
        => MicrosoftOptions.Create(new Options
        {
            Workers = workers,
            BufferSize = bufferSize,
            OutputPath = output ?? Path.GetTempFileName(),
            TargetSizeBytes = targetSizeBytes
        });
}