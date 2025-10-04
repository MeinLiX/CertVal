using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CertVal.EmailService.Services;

// experimental
public sealed class EmailServiceMetrics : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _messagesProcessedCounter;
    private readonly Counter<long> _messagesFailedCounter;
    private readonly Counter<long> _messagesRetriedCounter;
    private readonly Histogram<double> _messageProcessingDuration;
    private readonly Counter<long> _emailsSentCounter;
    private readonly Counter<long> _smtpConnectionsCreated;
    private readonly UpDownCounter<int> _queueDepth;

    public EmailServiceMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("CertVal.EmailService");

        _messagesProcessedCounter = _meter.CreateCounter<long>(
            "certval.email.messages.processed",
            description: "Total number of messages processed from queue");

        _messagesFailedCounter = _meter.CreateCounter<long>(
            "certval.email.messages.failed",
            description: "Total number of messages that failed processing");

        _messagesRetriedCounter = _meter.CreateCounter<long>(
            "certval.email.messages.retried",
            description: "Total number of messages queued for retry");

        _messageProcessingDuration = _meter.CreateHistogram<double>(
            "certval.email.message.processing.duration",
            unit: "ms",
            description: "Time taken to process a message");

        _emailsSentCounter = _meter.CreateCounter<long>(
            "certval.email.emails.sent",
            description: "Total number of emails sent successfully");

        _smtpConnectionsCreated = _meter.CreateCounter<long>(
            "certval.email.smtp.connections.created",
            description: "Total number of SMTP connections created");

        _queueDepth = _meter.CreateUpDownCounter<int>(
            "certval.email.queue.depth",
            description: "Current number of messages in queue");
    }

    public void RecordMessageProcessed(string messageType, bool success)
    {
        var tags = new TagList
        {
            { "message_type", messageType },
            { "success", success }
        };

        _messagesProcessedCounter.Add(1, tags);
    }

    public void RecordMessageFailed(string messageType, string reason)
    {
        var tags = new TagList
        {
            { "message_type", messageType },
            { "failure_reason", reason }
        };

        _messagesFailedCounter.Add(1, tags);
    }

    public void RecordMessageRetried(string messageType, int retryCount)
    {
        var tags = new TagList
        {
            { "message_type", messageType },
            { "retry_count", retryCount }
        };

        _messagesRetriedCounter.Add(1, tags);
    }

    public void RecordProcessingDuration(double durationMs, string messageType)
    {
        var tags = new TagList
        {
            { "message_type", messageType }
        };

        _messageProcessingDuration.Record(durationMs, tags);
    }

    public void RecordEmailSent(string messageType, bool isAggregated)
    {
        var tags = new TagList
        {
            { "message_type", messageType },
            { "aggregated", isAggregated }
        };

        _emailsSentCounter.Add(1, tags);
    }

    public void RecordSmtpConnectionCreated()
    {
        _smtpConnectionsCreated.Add(1);
    }

    public void UpdateQueueDepth(int depth)
    {
        _queueDepth.Add(depth);
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
