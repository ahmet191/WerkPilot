using System;
using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities
{
    public class TicketAiPrediction : AuditableEntity
    {
        public Guid TicketId { get; set; }
        
        /// <summary>
        /// The predicted category by the AI model.
        /// </summary>
        public string PredictedCategory { get; set; } = string.Empty;
        
        /// <summary>
        /// The predicted priority by the AI model.
        /// </summary>
        public string PredictedPriority { get; set; } = string.Empty;
        
        /// <summary>
        /// The confidence score of the prediction (e.g. 0.0 to 1.0)
        /// </summary>
        public double ConfidenceScore { get; set; }
        
        /// <summary>
        /// AI model generated reasoning for the prediction.
        /// </summary>
        public string Reasoning { get; set; } = string.Empty;

        // Navigation property (we'll assume Ticket exists)
        public Ticket? Ticket { get; set; }
    }
}
