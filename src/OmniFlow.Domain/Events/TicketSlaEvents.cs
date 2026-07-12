namespace OmniFlow.Domain.Events;

public class CheckTicketSlaBreachEvent
{
    /// <summary>Consumers run outside any HTTP context — they scope the DbContext with this.</summary>
    public Guid TenantId { get; set; }

    public Guid TicketId { get; set; }
    public DateTime ExpectedSlaResolutionTime { get; set; }
}
