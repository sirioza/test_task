using Microsoft.Extensions.Options;
using StringsGenerator.Data;
using StringsGenerator.Workers.Base;
using System.Buffers;
using System.Buffers.Text;
using System.Threading.Channels;

namespace StringsGenerator.Workers;

public class Producer(Channel<(byte[] buffer, int count)> channel, IOptions<Options> options)
    : ChannelWorker(options.Value)
{
    private readonly StringCollection _sc = new StringCollection().GetStrings().ConvertToBytePool().Build();
    private readonly Channel<(byte[] buffer, int count)> _channel = channel;
    private static readonly ThreadLocal<Random> ThreadRnd = new(() =>
        new Random(Environment.TickCount ^ Environment.CurrentManagedThreadId)
    );

    private static long _sharedTotalBytes;
    private static long _globalLineCounter;

    public override async Task RunAsync()
    {
        Random localRnd = ThreadRnd.Value!;

        while (true)
        {
            var buf = ArrayPool<byte>.Shared.Rent(base.BufferSize);
            int offset = 0;

            while (offset < buf.Length - 128)
            {
                byte[] stringBytes = _sc.StringPool[localRnd.Next(_sc.Length)];

                long lineNumber = Interlocked.Increment(ref _globalLineCounter);
                int numberSize = Utf8Formatter.TryFormat(lineNumber, buf.AsSpan(offset), out int written) ? written : 0;
                int entrySize = numberSize + 1 + stringBytes.Length + 1;

                long totalAfterAdd = Interlocked.Add(ref _sharedTotalBytes, entrySize);
                if (totalAfterAdd > base.TargetSizeBytes)
                {
                    break;
                }

                offset += numberSize;
                buf[offset++] = (byte)'.';
                stringBytes.CopyTo(buf.AsSpan(offset));
                offset += stringBytes.Length;
                buf[offset++] = (byte)'\r';
                buf[offset++] = (byte)'\n';
            }

            //Ideally, when the limit is reached
            //so that the producer does not try to write to an overflowing channel
            //add cancellation through CancellationToken
            if (offset > 0)
            {
                await _channel.Writer.WriteAsync((buf, offset));
            }
            else
            {
                ArrayPool<byte>.Shared.Return(buf);
                break;
            }
        }
    }
}
