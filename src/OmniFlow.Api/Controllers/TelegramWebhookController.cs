using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Api.Controllers;

// Anonymous by necessity (Telegram cannot authenticate), but every request must
// carry the webhook secret token registered via setWebhook.
[ApiController]
[Route("api/webhook/telegram")]
[AllowAnonymous]
public class TelegramWebhookController : ControllerBase
{
    private readonly IMessageGateway _messageGateway;
    private readonly ITelegramIdentityService _identityService;
    private readonly ILogger<TelegramWebhookController> _logger;

    public TelegramWebhookController(
        IMessageGateway messageGateway,
        ITelegramIdentityService identityService,
        ILogger<TelegramWebhookController> logger)
    {
        _messageGateway = messageGateway;
        _identityService = identityService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> HandleUpdate(CancellationToken cancellationToken)
    {
        // 1. Mandatory Telegram secret token validation.
        Request.Headers.TryGetValue("X-Telegram-Bot-Api-Secret-Token", out var secretToken);
        var isValid = await _identityService.ValidateWebhookPayloadAsync(string.Empty, secretToken.ToString());
        if (!isValid)
        {
            _logger.LogWarning("Webhook rejected: missing or mismatched secret token");
            return Unauthorized();
        }

        // 2. Read raw request body to pass to the gateway
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrEmpty(body))
        {
            return BadRequest("Payload cannot be empty");
        }

        // 3. Delegate webhook parsing and processing to channel-agnostic messaging gateway
        await _messageGateway.ReceiveMessageAsync(body, cancellationToken);

        return Ok();
    }
}
