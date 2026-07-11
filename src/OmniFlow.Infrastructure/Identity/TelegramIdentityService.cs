using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Infrastructure.Identity;

public class TelegramIdentityService : ITelegramIdentityService
{
    private readonly ILogger<TelegramIdentityService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public TelegramIdentityService(ILogger<TelegramIdentityService> logger, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
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
        // Telegram echoes back the secret_token supplied to setWebhook in the
        // X-Telegram-Bot-Api-Secret-Token header ("signature" here). If no secret
        // is configured, the webhook is considered disabled and everything is rejected.
        var configuredSecret = _configuration["Telegram:WebhookSecret"];
        if (string.IsNullOrEmpty(configuredSecret))
        {
            _logger.LogWarning("Telegram:WebhookSecret is not configured; rejecting webhook request");
            return Task.FromResult(false);
        }

        if (string.IsNullOrEmpty(signature))
        {
            return Task.FromResult(false);
        }

        var isValid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(configuredSecret),
            Encoding.UTF8.GetBytes(signature));

        return Task.FromResult(isValid);
    }
}
