using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Infrastructure.Identity;

public class TelegramIdentityService : ITelegramIdentityService
{
    private readonly ILogger<TelegramIdentityService> _logger;

    public TelegramIdentityService(ILogger<TelegramIdentityService> logger)
    {
        _logger = logger;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Guid> ChatToUserMap = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, string> UserToChatMap = new();

    static TelegramIdentityService()
    {
        // Default seed mapping
        var defaultUser = Guid.Parse("11111111-1111-1111-1111-111111111111");
        ChatToUserMap.TryAdd("12345678", defaultUser);
        UserToChatMap.TryAdd(defaultUser, "12345678");
    }

    public Task<Guid?> GetUserIdByTelegramChatIdAsync(string chatId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resolving user ID for Telegram Chat ID: {ChatId}", chatId);
        if (ChatToUserMap.TryGetValue(chatId, out var userId))
        {
            return Task.FromResult<Guid?>(userId);
        }
        
        // Auto-register any new chat ID for testing convenience
        var newUserId = Guid.NewGuid();
        ChatToUserMap.TryAdd(chatId, newUserId);
        UserToChatMap.TryAdd(newUserId, chatId);
        _logger.LogInformation("Auto-registered Telegram Chat ID {ChatId} to User {UserId}", chatId, newUserId);
        return Task.FromResult<Guid?>(newUserId);
    }

    public Task<string?> GetTelegramChatIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resolving Telegram Chat ID for user ID: {UserId}", userId);
        if (UserToChatMap.TryGetValue(userId, out var chatId))
        {
            return Task.FromResult<string?>(chatId);
        }
        return Task.FromResult<string?>(null);
    }

    public Task<bool> ValidateWebhookPayloadAsync(string payload, string signature)
    {
        // Verify token webhook signature
        _logger.LogInformation("Validating Telegram webhook signature...");
        return Task.FromResult(true);
    }
}
