using MediatR;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Users.Commands;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IOmniFlowDbContext _context;

    public CreateUserCommandHandler(IOmniFlowDbContext context)
    {
        _context = context;
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

        return user.Id;
    }
}
