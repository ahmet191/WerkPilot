using MediatR;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Users.Commands;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IOmniFlowDbContext _context;
    private readonly IIdentityAccountService _accounts;

    public CreateUserCommandHandler(IOmniFlowDbContext context, IIdentityAccountService accounts)
    {
        _context = context;
        _accounts = accounts;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Role = request.Role,
            DepartmentId = request.DepartmentId,
            TelegramChatId = request.TelegramChatId
        };

        // Note: ChangeTracker in DBContext interceptor will automatically assign Auditable fields
        _context.Set<User>().Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        // The login account shares the domain user's id. Passwordless + invite
        // email is the default; an admin-set initial password is also supported.
        await _accounts.CreateAccountAsync(
            user.Id,
            request.Email,
            request.InitialPassword,
            IdentityRoleMap.For(request.Role),
            request.SendInvite,
            cancellationToken);

        return user.Id;
    }
}
