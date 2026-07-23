using Xunit;
using Core.Application.Handlers;
using Core.Domain.Entities;

namespace Tests.UnitTests;

public class OutboxMessageTests
{
    [Fact]
    public void MarkRetry_ExceedsMax_MovesToDeadLetter()
    {
        var item = new OutboxMessage(SendMessageCommandHandler.OutboxTypeMessageSent, "{}");
        for (var i = 0; i < 3; i++)
            item.MarkRetry("fail", TimeSpan.FromSeconds(1), maxAttempts: 3);

        Assert.Equal(OutboxStatus.DeadLetter, item.Status);
        Assert.Equal(3, item.AttemptCount);
    }

    [Fact]
    public void MarkProcessed_SetsProcessedStatus()
    {
        var item = new OutboxMessage(SendMessageCommandHandler.OutboxTypeMessageSent, "{}");
        item.MarkProcessed();
        Assert.Equal(OutboxStatus.Processed, item.Status);
        Assert.NotNull(item.ProcessedAt);
    }
}
