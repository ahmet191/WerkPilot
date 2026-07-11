using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Departments.Queries;

public class DepartmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ManagerId { get; set; }
    public Guid? ParentDepartmentId { get; set; }
}

public class GetDepartmentsQuery : IRequest<List<DepartmentDto>>
{
}

public class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, List<DepartmentDto>>
{
    private readonly IOmniFlowDbContext _context;
    private readonly IUserContext _userContext;

    public GetDepartmentsQueryHandler(IOmniFlowDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    public async Task<List<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var departments = await _context.Departments.ToListAsync(cancellationToken);
        
        // Use the user's Accept-Language preference to resolve the name dynamically
        var lang = _userContext.Language;

        return departments.Select(d => new DepartmentDto
        {
            Id = d.Id,
            Name = d.LocalizedName.GetValueOrDefault(lang),
            ManagerId = d.ManagerId,
            ParentDepartmentId = d.ParentDepartmentId
        }).ToList();
    }
}
