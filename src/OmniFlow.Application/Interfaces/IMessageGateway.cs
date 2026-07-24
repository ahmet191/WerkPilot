namespace OmniFlow.Application.Interfaces;

public interface IMessageGateway
{
    Task SendMessageAsync(string chatId, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends to the configured support channel (Telegram <c>Telegram:SupportChatId</c>).
    /// No-ops when no channel is configured, so callers need not know or hardcode
    /// a destination.
    /// </summary>
    Task SendToSupportChannelAsync(string message, CancellationToken cancellationToken = default);

    Task ReceiveMessageAsync(string payload, CancellationToken cancellationToken = default);
}
