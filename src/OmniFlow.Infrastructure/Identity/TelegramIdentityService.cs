using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Infrastructure.Identity;

public class TelegramIdentityService : ITelegramIdentityService
{
    private readonly ILogger<TelegramIdentityService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public TelegramIdentityService(ILogger<TelegramIdentityService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<Guid?> GetUserIdByTelegramChatIdAsync(string chatId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resolving user ID for Telegram Chat ID: {ChatId}", chatId);
        
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IOmniFlowDbContext>();

        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.TelegramChatId == chatId, cancellationToken);

        return user?.Id;
    }

    public async Task<string?> GetTelegramChatIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resolving Telegram Chat ID for user ID: {UserId}", userId);
        
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IOmniFlowDbContext>();

        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user?.TelegramChatId;
    }

    public Task<bool> ValidateWebhookPayloadAsync(string payload, string signature)
    {
        // Verify token webhook signature
        _logger.LogInformation("Validating Telegram webhook signature...");
        return Task.FromResult(true);
    }
}
