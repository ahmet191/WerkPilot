using MediatR;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Common;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Departments.Commands;

public class CreateDepartmentCommand : IRequest<Guid>
{
    public string Name { get; set; } = string.Empty;
    public Guid? ManagerId { get; set; }
    public Guid? ParentDepartmentId { get; set; }
}

public class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, Guid>
{
    private readonly IOmniFlowDbContext _context;

    public CreateDepartmentCommandHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = new Department
        {
            Id = Guid.NewGuid(),
            // For now, save the same name to both languages or use a default
            LocalizedName = new MultiLingualString { Translations = new Dictionary<string, string> { { "en", request.Name }, { "tr", request.Name } } },
            ManagerId = request.ManagerId,
            ParentDepartmentId = request.ParentDepartmentId
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync(cancellationToken);

        return department.Id;
    }
}
