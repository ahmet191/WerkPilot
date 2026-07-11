using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Events;

namespace OmniFlow.Infrastructure.Messaging.Consumers;

public class AssetAssignedConsumer : IConsumer<AssetAssignedEvent>
{
    private readonly ILogger<AssetAssignedConsumer> _logger;
    private readonly IMessageGateway _messageGateway;
    private readonly IOmniFlowDbContext _context;

    public AssetAssignedConsumer(ILogger<AssetAssignedConsumer> logger, IMessageGateway messageGateway, IOmniFlowDbContext context)
    {
        _logger = logger;
        _messageGateway = messageGateway;
        _context = context;
    }

    public async Task Consume(ConsumeContext<AssetAssignedEvent> context)
    {
        var ev = context.Message;
        _logger.LogInformation("Processing AssetAssignedEvent for Asset {AssetId}", ev.AssetId);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == ev.AssignedToUserId);
        if (user != null && !string.IsNullOrEmpty(user.TelegramChatId))
        {
            var message = $"🚀 *New Asset Assigned*\n" +
                          $"You have been assigned a new IT Asset.\n" +
                          $"Model: {ev.Model}\n" +
                          $"Asset Tag: {ev.AssetTag}\n" +
                          $"Date: {ev.AssignedAt:yyyy-MM-dd HH:mm}";

            await _messageGateway.SendMessageAsync(user.TelegramChatId, message);
        }
    }
}
