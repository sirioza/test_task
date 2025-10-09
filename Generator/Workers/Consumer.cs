using Microsoft.Extensions.Options;
using StringsGenerator.Extensions;
using StringsGenerator.Workers.Base;
using System.Buffers;
using System.Threading.Channels;

namespace StringsGenerator.Workers;

public class Consumer(Channel<(byte[] buffer, int count)> channel, IOptions<Options> options)
    : ChannelWorker(options.Value)
{
    private readonly string _path = options.Value.OutputPath.NotNull(nameof(Options.OutputPath));
    private readonly Channel<(byte[] buffer, int count)> _channel = channel;

    public override async Task RunAsync()
    {
        await using var fs = new FileStream(
            _path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            base.BufferSize,
            FileOptions.None);

        long bytesWritten = 0;

        await foreach ((byte[] buf, int count) in _channel.Reader.ReadAllAsync())
        {
            int toWrite = (int)Math.Min(count, TargetSizeBytes - bytesWritten);
            if (toWrite <= 0)
            {
                ArrayPool<byte>.Shared.Return(buf);
                break;
            }

            await fs.WriteAsync(buf.AsMemory(0, toWrite));
            bytesWritten += toWrite;

            ArrayPool<byte>.Shared.Return(buf);

            if (bytesWritten >= TargetSizeBytes)
            {
                break;
            }
        }
    }
}