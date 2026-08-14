using System.Text.Json;
using Core.Application.Handlers;
using Core.Application.Interfaces;
using Core.Application.Logging;
using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebAPI.Services;

/// <summary>Polls transactional outbox and delivers message notifications (SignalR/push).</summary>
public class OutboxProcessorHostedService : BackgroundService
{
    private const int BatchSize = 20;
    private const int MaxAttempts = 10;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorHostedService> _logger;

    public OutboxProcessorHostedService(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessorHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                AppLog.Error(_logger, AppLogEvents.OutboxProcessorFailed, ex,
                    "Error: {ErrorType} | Result: Failure",
                    ex.GetType().Name);
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notifier = scope.ServiceProvider.GetRequiredService<IMessageNotificationService>();

        var pending = await outbox.GetPendingAsync(BatchSize, cancellationToken);
        foreach (var item in pending)
        {
            try
            {
                await DispatchAsync(notifier, item);
                item.MarkProcessed();
                await outbox.UpdateAsync(item, cancellationToken);
                await unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                AppLog.Warning(_logger, AppLogEvents.OutboxItemFailed, ex,
                    "OutboxId: {OutboxId} | Attempt: {Attempt} | Error: {ErrorType} | Result: Failure",
                    item.Id, item.AttemptCount + 1, ex.GetType().Name);
                var delay = TimeSpan.FromSeconds(Math.Min(300, Math.Pow(2, item.AttemptCount + 1)));
                item.MarkRetry(ex.Message, delay, MaxAttempts);
                await outbox.UpdateAsync(item, cancellationToken);
                await unitOfWork.SaveChangesAsync();
            }
        }
    }

    private static async Task DispatchAsync(IMessageNotificationService notifier, OutboxMessage item)
    {
        if (item.Type == SendMessageCommandHandler.OutboxTypeMessageSent)
        {
            var payload = JsonSerializer.Deserialize<MessageSentOutboxPayload>(item.Payload)
                ?? throw new InvalidOperationException("Invalid MessageSent outbox payload");

            await notifier.SendMessage(
                payload.MessageId,
                payload.SenderId,
                payload.ReceiverId,
                payload.GroupId,
                payload.Content,
                payload.SentAt,
                payload.AttachmentUrl,
                payload.AttachmentType,
                payload.BranchId,
                payload.ReplyToMessageId,
                payload.SupportSenderId);
            return;
        }

        if (item.Type == EditMessageCommandHandler.OutboxTypeMessageEdited)
        {
            var payload = JsonSerializer.Deserialize<MessageEditedOutboxPayload>(item.Payload)
                ?? throw new InvalidOperationException("Invalid MessageEdited outbox payload");
            await notifier.NotifyMessageEdited(
                payload.MessageId, payload.SenderId, payload.ReceiverId, payload.GroupId, payload.NewContent, payload.EditedAt);
            return;
        }

        if (item.Type == DeleteMessageCommandHandler.OutboxTypeMessageDeleted)
        {
            var payload = JsonSerializer.Deserialize<MessageDeletedOutboxPayload>(item.Payload)
                ?? throw new InvalidOperationException("Invalid MessageDeleted outbox payload");
            await notifier.NotifyMessageDeleted(
                payload.MessageId, payload.SenderId, payload.ReceiverId, payload.GroupId, payload.DeletedAt);
            return;
        }

        if (item.Type == AddOrUpdateReactionCommandHandler.OutboxTypeReactionsUpdated)
        {
            var payload = JsonSerializer.Deserialize<MessageReactionsOutboxPayload>(item.Payload)
                ?? throw new InvalidOperationException("Invalid MessageReactionsUpdated outbox payload");
            await notifier.NotifyMessageReactionsUpdated(payload.MessageId);
            return;
        }

        throw new InvalidOperationException($"Unknown outbox type: {item.Type}");
    }
}
