using MediatR;
using OmniFlow.Domain.Common.Exceptions;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Users.Commands;

public class DeleteUserCommand : IRequest
{
    public Guid Id { get; set; }
}

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IOmniFlowDbContext _context;

    public DeleteUserCommandHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { request.Id }, cancellationToken);
        if (user == null) throw new NotFoundException("User", request.Id);

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
