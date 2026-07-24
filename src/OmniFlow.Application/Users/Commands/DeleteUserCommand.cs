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
    private readonly IIdentityAccountService _accounts;

    public DeleteUserCommandHandler(IOmniFlowDbContext context, IIdentityAccountService accounts)
    {
        _context = context;
        _accounts = accounts;
    }

    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { request.Id }, cancellationToken);
        if (user == null) throw new NotFoundException("User", request.Id);

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);

        // Soft-deleted users must not keep a working login: disable the account
        // if one exists (ignore missing accounts).
        var status = await _accounts.GetStatusAsync(user.Id, cancellationToken);
        if (status.HasAccount)
        {
            await _accounts.DisableAsync(user.Id, actingUserId: Guid.Empty, cancellationToken);
        }
    }
}
