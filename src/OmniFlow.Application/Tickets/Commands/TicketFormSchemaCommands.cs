using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Tickets.Commands
{
    public class TicketFormSchemaDto
    {
        public Guid Id { get; set; }
        public Guid ServiceDeskId { get; set; }
        public string ServiceDeskName { get; set; } = string.Empty;
        public string FormTitle { get; set; } = string.Empty;
        public string FormDescription { get; set; } = string.Empty;
        public string FieldsJson { get; set; } = "[]";
        public bool IsActive { get; set; }
    }

    public record GetTicketFormSchemasQuery(Guid TenantId) : IRequest<List<TicketFormSchemaDto>>;

    public class GetTicketFormSchemasQueryHandler : IRequestHandler<GetTicketFormSchemasQuery, List<TicketFormSchemaDto>>
    {
        private readonly IOmniFlowDbContext _context;

        public GetTicketFormSchemasQueryHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<List<TicketFormSchemaDto>> Handle(GetTicketFormSchemasQuery request, CancellationToken cancellationToken)
        {
            var schemas = await _context.TicketFormSchemas
                .AsNoTracking()
                .Include(f => f.ServiceDesk)
                .Where(f => f.TenantId == request.TenantId)
                .Select(f => new TicketFormSchemaDto
                {
                    Id = f.Id,
                    ServiceDeskId = f.ServiceDeskId,
                    ServiceDeskName = f.ServiceDesk != null ? f.ServiceDesk.Name : "General Service Desk",
                    FormTitle = f.FormTitle,
                    FormDescription = f.FormDescription,
                    FieldsJson = f.FieldsJson,
                    IsActive = f.IsActive
                })
                .ToListAsync(cancellationToken);

            // Default seed schema for demonstration if empty
            if (!schemas.Any())
            {
                var desk = await _context.ServiceDesks.FirstOrDefaultAsync(cancellationToken);
                var deskId = desk?.Id ?? Guid.NewGuid();
                var deskName = desk?.Name ?? "IT & Corporate Support";

                return new List<TicketFormSchemaDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ServiceDeskId = deskId,
                        ServiceDeskName = deskName,
                        FormTitle = "Hardware & Asset Request Form",
                        FormDescription = "Request laptop, monitor, or peripheral device with approval workflow.",
                        FieldsJson = """
                        [
                            { "key": "asset_type", "label": "Device Type", "fieldType": "Select", "isRequired": true, "options": ["MacBook Pro 16", "Dell XPS 15", "27 4K Monitor", "Ergonomic Keyboard"] },
                            { "key": "justification", "label": "Business Justification", "fieldType": "TextArea", "isRequired": true },
                            { "key": "needed_by_date", "label": "Required By Date", "fieldType": "Date", "isRequired": false }
                        ]
                        """,
                        IsActive = true
                    }
                };
            }

            return schemas;
        }
    }

    public class SaveTicketFormSchemaCommand : IRequest<Guid>
    {
        public Guid? Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid ServiceDeskId { get; set; }
        public string FormTitle { get; set; } = string.Empty;
        public string FormDescription { get; set; } = string.Empty;
        public string FieldsJson { get; set; } = "[]";
        public bool IsActive { get; set; } = true;
    }

    public class SaveTicketFormSchemaCommandHandler : IRequestHandler<SaveTicketFormSchemaCommand, Guid>
    {
        private readonly IOmniFlowDbContext _context;

        public SaveTicketFormSchemaCommandHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(SaveTicketFormSchemaCommand request, CancellationToken cancellationToken)
        {
            TicketFormSchema schema;
            if (request.Id.HasValue && request.Id.Value != Guid.Empty)
            {
                schema = await _context.TicketFormSchemas.FirstOrDefaultAsync(s => s.Id == request.Id.Value, cancellationToken)
                    ?? new TicketFormSchema { TenantId = request.TenantId, ServiceDeskId = request.ServiceDeskId };
            }
            else
            {
                schema = new TicketFormSchema { TenantId = request.TenantId, ServiceDeskId = request.ServiceDeskId };
                _context.TicketFormSchemas.Add(schema);
            }

            schema.FormTitle = request.FormTitle;
            schema.FormDescription = request.FormDescription;
            schema.FieldsJson = request.FieldsJson;
            schema.IsActive = request.IsActive;

            await _context.SaveChangesAsync(cancellationToken);
            return schema.Id;
        }
    }
}
