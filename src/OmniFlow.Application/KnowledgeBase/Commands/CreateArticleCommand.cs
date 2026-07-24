using MediatR;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Common;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.KnowledgeBase.Commands;

public class CreateArticleCommand : IRequest<Guid>
{
    public Dictionary<string, string> LocalizedTitle { get; set; } = new();
    public Dictionary<string, string> LocalizedContent { get; set; } = new();
    public Guid DepartmentId { get; set; }
    public Guid? CategoryId { get; set; }
    public bool IsPublished { get; set; }
}

public class CreateArticleCommandHandler : IRequestHandler<CreateArticleCommand, Guid>
{
    private readonly IOmniFlowDbContext _context;

    public CreateArticleCommandHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateArticleCommand request, CancellationToken cancellationToken)
    {
        var article = new KnowledgeBaseArticle
        {
            Id = Guid.NewGuid(),
            LocalizedTitle = new MultiLingualString { Translations = request.LocalizedTitle },
            LocalizedContent = new MultiLingualString { Translations = request.LocalizedContent },
            DepartmentId = request.DepartmentId,
            CategoryId = request.CategoryId,
            IsPublished = request.IsPublished,
            ViewCount = 0
        };

        _context.KnowledgeBaseArticles.Add(article);
        await _context.SaveChangesAsync(cancellationToken);

        return article.Id;
    }
}
