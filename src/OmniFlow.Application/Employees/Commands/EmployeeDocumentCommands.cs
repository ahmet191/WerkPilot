using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Employees.Commands
{
    public class EmployeeDocumentDto
    {
        public Guid Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsExpired => ExpirationDate.HasValue && ExpirationDate.Value < DateTime.UtcNow;
        public bool IsExpiringSoon => ExpirationDate.HasValue && ExpirationDate.Value >= DateTime.UtcNow && ExpirationDate.Value <= DateTime.UtcNow.AddDays(30);
    }

    public record GetMyEmployeeDocumentsQuery(Guid UserId) : IRequest<List<EmployeeDocumentDto>>;

    public class GetMyEmployeeDocumentsQueryHandler : IRequestHandler<GetMyEmployeeDocumentsQuery, List<EmployeeDocumentDto>>
    {
        private readonly IOmniFlowDbContext _context;

        public GetMyEmployeeDocumentsQueryHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<List<EmployeeDocumentDto>> Handle(GetMyEmployeeDocumentsQuery request, CancellationToken cancellationToken)
        {
            return await _context.EmployeeDocuments
                .AsNoTracking()
                .Where(d => d.UserId == request.UserId)
                .OrderByDescending(d => d.UploadedAt)
                .Select(d => new EmployeeDocumentDto
                {
                    Id = d.Id,
                    Category = d.Category.ToString(),
                    Title = d.Title,
                    FileName = d.FileName,
                    FileSizeBytes = d.FileSizeBytes,
                    ExpirationDate = d.ExpirationDate,
                    UploadedAt = d.UploadedAt
                })
                .ToListAsync(cancellationToken);
        }
    }

    public class UploadEmployeeDocumentCommand : IRequest<Guid>
    {
        public Guid UserId { get; set; }
        public string Category { get; set; } = "Contract";
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public Stream Content { get; set; } = Stream.Null;
        public string ContentType { get; set; } = "application/pdf";
        public DateTime? ExpirationDate { get; set; }
    }

    public class UploadEmployeeDocumentCommandHandler : IRequestHandler<UploadEmployeeDocumentCommand, Guid>
    {
        private readonly IOmniFlowDbContext _context;
        private readonly IFileStorage _storage;

        public UploadEmployeeDocumentCommandHandler(IOmniFlowDbContext context, IFileStorage storage)
        {
            _context = context;
            _storage = storage;
        }

        public async Task<Guid> Handle(UploadEmployeeDocumentCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            if (user == null)
            {
                throw new KeyNotFoundException($"User '{request.UserId}' not found.");
            }

            var storageKey = await _storage.SaveAsync(user.TenantId, request.FileName, request.Content, cancellationToken);

            Enum.TryParse<DocumentCategory>(request.Category, true, out var cat);

            var doc = new EmployeeDocument
            {
                TenantId = user.TenantId,
                UserId = request.UserId,
                Category = cat,
                Title = string.IsNullOrWhiteSpace(request.Title) ? request.FileName : request.Title,
                FileName = request.FileName,
                FileStorageKey = storageKey,
                FileSizeBytes = request.Content.Length,
                ContentType = request.ContentType,
                ExpirationDate = request.ExpirationDate,
                UploadedAt = DateTime.UtcNow
            };

            _context.EmployeeDocuments.Add(doc);
            await _context.SaveChangesAsync(cancellationToken);

            return doc.Id;
        }
    }
}
