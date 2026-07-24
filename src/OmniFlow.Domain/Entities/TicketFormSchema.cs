using System;
using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities
{
    public class TicketFormSchema : AuditableEntity
    {
        public Guid ServiceDeskId { get; set; }
        public string FormTitle { get; set; } = string.Empty;
        public string FormDescription { get; set; } = string.Empty;
        /// <summary>
        /// JSON payload storing dynamic form fields: Key, Label, FieldType, IsRequired, Options, OrderIndex.
        /// </summary>
        public string FieldsJson { get; set; } = "[]";
        public bool IsActive { get; set; } = true;

        // Navigation
        public ServiceDesk? ServiceDesk { get; set; }
    }
}
