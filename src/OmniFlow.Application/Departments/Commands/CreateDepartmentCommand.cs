using MediatR;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Common;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Departments.Commands;

public class CreateDepartmentCommand : IRequest<Guid>
{
    // Admin sends the dictionary to define all translations
    public Dictionary<string, string> LocalizedName { get; set; } = new();
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
            LocalizedName = new MultiLingualString { Translations = request.LocalizedName },
            ManagerId = request.ManagerId,
            ParentDepartmentId = request.ParentDepartmentId
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync(cancellationToken);

        return department.Id;
    }
}
