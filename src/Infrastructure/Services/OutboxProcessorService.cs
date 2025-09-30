using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sigma.Domain.Contracts;
using Sigma.Domain.Repositories;
using Sigma.Infrastructure.Persistence;
using System.Text.Json;

namespace Sigma.Infrastructure.Services;

public class OutboxProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly TimeSpan _processInterval = TimeSpan.FromSeconds(10);

    public OutboxProcessorService(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessorService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_processInterval, stoppingToken);
        }
    }

    private async Task ProcessOutboxMessages(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var messages = await outboxRepository.GetUnprocessedAsync(100, cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                // Process the message based on event type
                await ProcessMessage(message, scope.ServiceProvider, cancellationToken);

                message.MarkAsProcessed();
                await outboxRepository.UpdateAsync(message, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully processed outbox message {MessageId} of type {EventType}",
                    message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process outbox message {MessageId} of type {EventType}",
                    message.Id, message.EventType);

                message.MarkAsFailed(ex.Message);
                await outboxRepository.UpdateAsync(message, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task ProcessMessage(
        Domain.Entities.OutboxMessage message,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        // This is where you would dispatch to appropriate handlers based on event type
        // For now, we'll just log the event
        _logger.LogInformation(
            "Processing domain event: Type={EventType}, Data={EventData}",
            message.EventType, message.EventData);

        // In a real implementation, you would:
        // 1. Deserialize the event data to the appropriate type
        // 2. Resolve the appropriate handler from DI
        // 3. Execute the handler
        // Example:
        // var eventType = Type.GetType(message.EventType);
        // var eventData = JsonSerializer.Deserialize(message.EventData, eventType);
        // var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        // var handler = serviceProvider.GetService(handlerType);
        // await handler.Handle(eventData, cancellationToken);

        await Task.CompletedTask;
    }
}