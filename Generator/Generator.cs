using Microsoft.Extensions.Options;
using StringsGenerator.Extensions;
using StringsGenerator.Workers.Base;
using System.Threading.Channels;

namespace StringsGenerator;

public class Generator(Channel<(byte[] buffer, int count)> channel, IChannelWorker consumer, IChannelWorker producer,
    IOptions<Options> options)
{
    private readonly IChannelWorker _consumer = consumer;
    private readonly IChannelWorker _producer = producer;
    private readonly Channel<(byte[] buffer, int count)> _channel = channel;
    private readonly byte _workers = options.Value.Workers.NotZero(nameof(options.Value.Workers));

    public async Task GenerateAsync()
    {
        Task consumerTask = _consumer.RunAsync();

        Task[] producerTasks = new Task[_workers];
        for (int w = 0; w < _workers; w++)
        {
            producerTasks[w] = _producer.RunAsync();
        }
        await Task.WhenAll(producerTasks);
        _channel.Writer.Complete();

        await consumerTask;
    }
}
