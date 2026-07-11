namespace OmniFlow.Domain.Events;

public class CheckTicketSlaBreachEvent
{
    public Guid TicketId { get; set; }
    public DateTime ExpectedSlaResolutionTime { get; set; }
}
