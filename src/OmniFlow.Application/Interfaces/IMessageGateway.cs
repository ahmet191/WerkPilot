namespace OmniFlow.Application.Interfaces;

public interface IMessageGateway
{
    Task SendMessageAsync(string chatId, string message, CancellationToken cancellationToken = default);
    Task ReceiveMessageAsync(string payload, CancellationToken cancellationToken = default);
}
