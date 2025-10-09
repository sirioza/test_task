using StringsGenerator.Extensions;

namespace StringsGenerator.Workers.Base;

public abstract class ChannelWorker(Options options) : IChannelWorker
{
    protected readonly long TargetSizeBytes = options.TargetSizeBytes.NotZero(nameof(Options.TargetSizeBytes));
    protected readonly int BufferSize = options.BufferSize.NotZero(nameof(Options.BufferSize));

    public abstract Task RunAsync();
}
