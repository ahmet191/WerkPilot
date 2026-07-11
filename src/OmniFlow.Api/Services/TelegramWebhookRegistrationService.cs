using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace OmniFlow.Api.Services;

public class TelegramWebhookRegistrationService : IHostedService
{
    private readonly ITelegramBotClient? _botClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramWebhookRegistrationService> _logger;

    public TelegramWebhookRegistrationService(
        IConfiguration configuration,
        ILogger<TelegramWebhookRegistrationService> logger,
        ITelegramBotClient? botClient = null)
    {
        _configuration = configuration;
        _logger = logger;
        _botClient = botClient;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Execute registration in the background to prevent blocking host startup
        _ = Task.Run(async () =>
        {
            if (_botClient == null)
            {
                _logger.LogWarning("Telegram Bot Client is not registered. Skipping webhook registration.");
                return;
            }

            var webhookUrl = _configuration["Telegram:WebhookUrl"];
            if (string.IsNullOrEmpty(webhookUrl))
            {
                _logger.LogWarning("Telegram WebhookUrl is not configured. Skipping webhook registration.");
                return;
            }

            _logger.LogInformation("Registering Telegram webhook to: {Url}...", webhookUrl);

            try
            {
                // We pass CancellationToken.None to let it complete even if host token cancellation is requested immediately
                await _botClient.SetWebhook(
                    url: webhookUrl,
                    cancellationToken: CancellationToken.None);
                _logger.LogInformation("Telegram webhook registered successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register Telegram webhook.");
            }
        });

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // Option to delete webhook on shutdown, usually skipped for persistence, but we can log
        _logger.LogInformation("Stopping Webhook Registration Service.");
        await Task.CompletedTask;
    }
}
