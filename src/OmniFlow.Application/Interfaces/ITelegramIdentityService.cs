namespace OmniFlow.Application.Interfaces;

public interface ITelegramIdentityService
{
    Task<Guid?> GetUserIdByTelegramChatIdAsync(string chatId, CancellationToken cancellationToken = default);
    Task<string?> GetTelegramChatIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ValidateWebhookPayloadAsync(string payload, string signature);
}
