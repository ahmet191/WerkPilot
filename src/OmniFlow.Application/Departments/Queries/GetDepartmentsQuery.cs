using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Common.Grid;
using OmniFlow.Application.Common.Models;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Departments.Queries;

public class DepartmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ManagerId { get; set; }
    public string ManagerName { get; set; } = string.Empty;
    public Guid? ParentDepartmentId { get; set; }
    public string ParentDepartmentName { get; set; } = string.Empty;
}

public record GetDepartmentsQuery(GridRequest Request) : IRequest<PagedResponse<DepartmentDto>>;

public class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, PagedResponse<DepartmentDto>>
{
    private readonly IOmniFlowDbContext _context;
    private readonly IUserContext _userContext;

    public GetDepartmentsQueryHandler(IOmniFlowDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    public async Task<PagedResponse<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var departments = await _context.Departments
            .Include(d => d.Manager)
            .Include(d => d.ParentDepartment)
            .ToListAsync(cancellationToken);

        var lang = _userContext.Language;

        // Localized JSONB projection — grid ops applied server-side in memory.
        return departments.Select(d => new DepartmentDto
        {
            Id = d.Id,
            Name = d.LocalizedName.GetValueOrDefault(lang),
            ManagerId = d.ManagerId,
            ManagerName = d.Manager != null ? d.Manager.Name : string.Empty,
            ParentDepartmentId = d.ParentDepartmentId,
            ParentDepartmentName = d.ParentDepartment != null ? d.ParentDepartment.LocalizedName.GetValueOrDefault(lang) : string.Empty
        }).ToGridResult(request.Request, [nameof(DepartmentDto.Name), nameof(DepartmentDto.ManagerName), nameof(DepartmentDto.ParentDepartmentName)]);
    }
}
