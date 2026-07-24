using System;
using System.Threading;
using System.Threading.Tasks;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Interfaces
{
    public interface IAiTriageService
    {
        Task<TicketAiPrediction> PredictTicketAsync(string title, string description, CancellationToken cancellationToken);
    }
}
