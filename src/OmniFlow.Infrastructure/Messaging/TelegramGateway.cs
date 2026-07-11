using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Tickets.Commands;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Text.Json;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Messaging;

public class TelegramGateway : IMessageGateway
{
    private readonly ILogger<TelegramGateway> _logger;
    private readonly ITelegramBotClient? _botClient;
    private readonly ITelegramIdentityService _identityService;
    private readonly IServiceProvider _serviceProvider;

    public TelegramGateway(
        ILogger<TelegramGateway> logger,
        ITelegramIdentityService identityService,
        IServiceProvider serviceProvider,
        ITelegramBotClient? botClient = null)
    {
        _logger = logger;
        _identityService = identityService;
        _serviceProvider = serviceProvider;
        _botClient = botClient;
    }

    public async Task SendMessageAsync(string chatId, string message, CancellationToken cancellationToken = default)
    {
        if (_botClient != null)
        {
            try
            {
                if (long.TryParse(chatId, out long numericChatId))
                {
                    await _botClient.SendMessage(
                        chatId: numericChatId,
                        text: message,
                        cancellationToken: cancellationToken);
                    _logger.LogInformation("Sent Telegram message to {ChatId}", chatId);
                }
                else
                {
                    _logger.LogWarning("Invalid numeric chat ID format: {ChatId}", chatId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Telegram message to {ChatId}", chatId);
            }
        }
        else
        {
            _logger.LogWarning("[MOCK] Sending Telegram message to {ChatId}: {Message}", chatId, message);
        }
    }

    public async Task ReceiveMessageAsync(string payload, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received Telegram webhook payload processing...");

        try
        {
            var update = JsonSerializer.Deserialize<Update>(payload, Telegram.Bot.JsonBotAPI.Options);
            if (update?.Message == null)
            {
                _logger.LogWarning("Telegram update or message was null after deserialization. Payload: {Payload}", payload);
                return;
            }

            var message = update.Message;
            var chatId = message.Chat.Id.ToString();
            var text = message.Text?.Trim();

            if (string.IsNullOrEmpty(text)) return;

            _logger.LogInformation("Telegram Message from {ChatId}: {Text}", chatId, text);

            // 1. Authenticate / Resolve Internal User ID
            var userId = await _identityService.GetUserIdByTelegramChatIdAsync(chatId, cancellationToken);
            if (userId == null)
            {
                await SendMessageAsync(chatId, "Unauthorized: Please link your Telegram account with HR.", cancellationToken);
                return;
            }

            // 2. Handle commands
            if (text.StartsWith("/start"))
            {
                await SendMessageAsync(chatId, "Welcome to OmniFlow!\n- Use `/ticket <TemplateId> <Field1=Val1>|<Field2=Val2>` to create a ticket.\n- Use `/list` to view your tickets.\n- Use `/transition <TicketId> <Trigger>` to update status.", cancellationToken);
            }
            else if (text.StartsWith("/ticket"))
            {
                await HandleCreateTicketCommandAsync(chatId, userId.Value, text, cancellationToken);
            }
            else if (text.StartsWith("/list"))
            {
                await HandleListTicketsQueryAsync(chatId, userId.Value, cancellationToken);
            }
            else if (text.StartsWith("/transition"))
            {
                await HandleTransitionTicketCommandAsync(chatId, userId.Value, text, cancellationToken);
            }
            else
            {
                await SendMessageAsync(chatId, $"Echo: {text}. Type `/start` to see available commands.", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook update payload");
        }
    }

    private async Task HandleCreateTicketCommandAsync(string chatId, Guid userId, string commandText, CancellationToken cancellationToken)
    {
        // Example: /ticket 8f8e02d6-444f-4d37-88ab-8c90ad74ef1a Title=Laptop broken|Description=Screen is black
        var parts = commandText.Split(' ', 3);
        if (parts.Length < 3)
        {
            await SendMessageAsync(chatId, "Usage: `/ticket <TemplateGuid> <Field1=Value1>|<Field2=Value2>`\nExample: `/ticket 2e3e5... Priority=High|Issue=Internet offline`", cancellationToken);
            return;
        }

        if (!Guid.TryParse(parts[1], out Guid templateId))
        {
            await SendMessageAsync(chatId, "Invalid Template ID format. Must be a GUID.", cancellationToken);
            return;
        }

        var dataPairs = parts[2].Split('|');
        var dynamicData = new Dictionary<string, object>();
        foreach (var pair in dataPairs)
        {
            var kv = pair.Split('=', 2);
            if (kv.Length == 2)
            {
                dynamicData[kv[0].Trim()] = kv[1].Trim();
            }
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new CreateTicketCommand(templateId, userId, dynamicData);
            var ticketId = await mediator.Send(command, cancellationToken);

            await SendMessageAsync(chatId, $"✅ Ticket created successfully!\nID: `{ticketId}`", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create ticket via MediatR");
            await SendMessageAsync(chatId, $"❌ Error creating ticket: {ex.Message}", cancellationToken);
        }
    }

    private async Task HandleListTicketsQueryAsync(string chatId, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var query = new OmniFlow.Application.Tickets.Queries.GetTicketsQuery(userId);
            var tickets = await mediator.Send(query, cancellationToken);

            if (tickets == null || tickets.Count == 0)
            {
                await SendMessageAsync(chatId, "You have no tickets.", cancellationToken);
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("📋 **Your Tickets:**");
            foreach (var ticket in tickets)
            {
                sb.AppendLine($"• `{ticket.Id}` - State: *{ticket.CurrentState}* (Created: {ticket.CreatedAt:g})");
            }

            await SendMessageAsync(chatId, sb.ToString(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list tickets");
            await SendMessageAsync(chatId, $"❌ Error fetching tickets: {ex.Message}", cancellationToken);
        }
    }

    private async Task HandleTransitionTicketCommandAsync(string chatId, Guid userId, string commandText, CancellationToken cancellationToken)
    {
        // Example: /transition <TicketGuid> StartWork
        var parts = commandText.Split(' ', 3);
        if (parts.Length < 3)
        {
            await SendMessageAsync(chatId, "Usage: `/transition <TicketGuid> <Trigger>`\nExample: `/transition <Guid> StartWork` (Valid triggers: StartWork, RequestApproval, Approve, Reject, Close)", cancellationToken);
            return;
        }

        if (!Guid.TryParse(parts[1], out Guid ticketId))
        {
            await SendMessageAsync(chatId, "Invalid Ticket ID format. Must be a GUID.", cancellationToken);
            return;
        }

        var trigger = parts[2].Trim();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new TransitionTicketCommand(ticketId, trigger, userId);
            var success = await mediator.Send(command, cancellationToken);

            if (success)
            {
                await SendMessageAsync(chatId, "✅ State transition requested successfully.", cancellationToken);
            }
            else
            {
                await SendMessageAsync(chatId, "❌ Failed to request state transition.", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transition ticket");
            await SendMessageAsync(chatId, $"❌ Error: {ex.Message}", cancellationToken);
        }
    }
}
