using MediatR;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Departments.Commands;

public class DeleteDepartmentCommand : IRequest
{
    public Guid Id { get; set; }
}

public class DeleteDepartmentCommandHandler : IRequestHandler<DeleteDepartmentCommand>
{
    private readonly IOmniFlowDbContext _context;

    public DeleteDepartmentCommandHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await _context.Departments.FindAsync(new object[] { request.Id }, cancellationToken);
        if (department == null) throw new Exception("Department not found");

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
