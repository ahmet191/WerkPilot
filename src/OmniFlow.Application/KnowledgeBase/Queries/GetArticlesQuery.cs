using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.KnowledgeBase.Queries;

public class KnowledgeBaseArticleDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Guid? CategoryId { get; set; }
    public bool IsPublished { get; set; }
    public int ViewCount { get; set; }
}

public class GetArticlesQuery : IRequest<List<KnowledgeBaseArticleDto>>
{
    public bool OnlyPublished { get; set; } = true;
}

public class GetArticlesQueryHandler : IRequestHandler<GetArticlesQuery, List<KnowledgeBaseArticleDto>>
{
    private readonly IOmniFlowDbContext _context;
    private readonly IUserContext _userContext;

    public GetArticlesQueryHandler(IOmniFlowDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    public async Task<List<KnowledgeBaseArticleDto>> Handle(GetArticlesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.KnowledgeBaseArticles.AsQueryable();

        if (request.OnlyPublished)
        {
            query = query.Where(k => k.IsPublished);
        }

        var articles = await query.ToListAsync(cancellationToken);
        var lang = _userContext.Language;

        return articles.Select(a => new KnowledgeBaseArticleDto
        {
            Id = a.Id,
            Title = a.LocalizedTitle.GetValueOrDefault(lang),
            Content = a.LocalizedContent.GetValueOrDefault(lang),
            DepartmentId = a.DepartmentId,
            CategoryId = a.CategoryId,
            IsPublished = a.IsPublished,
            ViewCount = a.ViewCount
        }).ToList();
    }
}
