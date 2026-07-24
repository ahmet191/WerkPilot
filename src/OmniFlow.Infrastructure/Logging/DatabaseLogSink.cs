using System.Threading.Channels;
using Serilog.Core;
using Serilog.Events;

namespace OmniFlow.Infrastructure.Logging;

/// <summary>
/// A non-blocking Serilog sink that pushes log events into an unbounded channel.
/// A background hosted service dequeues these and writes them to PostgreSQL.
/// </summary>
public class DatabaseLogSink : ILogEventSink
{
    private readonly ChannelWriter<LogEvent> _channelWriter;
    private readonly IFormatProvider? _formatProvider;

    public DatabaseLogSink(ChannelWriter<LogEvent> channelWriter, IFormatProvider? formatProvider = null)
    {
        _channelWriter = channelWriter;
        _formatProvider = formatProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        // Try to write immediately; unbounded channels rarely fail this unless closed.
        _channelWriter.TryWrite(logEvent);
    }
}
