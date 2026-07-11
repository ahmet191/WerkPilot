using MediatR;
using OmniFlow.Application.Common.Exceptions;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Departments.Commands;

public class UpdateDepartmentCommand : IRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ManagerId { get; set; }
    public Guid? ParentDepartmentId { get; set; }
}

public class UpdateDepartmentCommandHandler : IRequestHandler<UpdateDepartmentCommand>
{
    private readonly IOmniFlowDbContext _context;

    public UpdateDepartmentCommandHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await _context.Departments.FindAsync(new object[] { request.Id }, cancellationToken);
        if (department == null) throw new NotFoundException("Department", request.Id);

        if (department.LocalizedName != null && department.LocalizedName.Translations != null)
        {
            department.LocalizedName.Translations["en"] = request.Name;
            department.LocalizedName.Translations["tr"] = request.Name;
        }

        department.ManagerId = request.ManagerId;
        department.ParentDepartmentId = request.ParentDepartmentId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
