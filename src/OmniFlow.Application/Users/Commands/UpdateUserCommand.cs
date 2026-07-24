using MediatR;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Common.Exceptions;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Users.Commands;

public class UpdateUserCommand : IRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid DepartmentId { get; set; }
    public string? TelegramChatId { get; set; }
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand>
{
    private readonly IOmniFlowDbContext _context;
    private readonly IIdentityAccountService _accounts;

    public UpdateUserCommandHandler(IOmniFlowDbContext context, IIdentityAccountService accounts)
    {
        _context = context;
        _accounts = accounts;
    }

    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { request.Id }, cancellationToken);
        if (user == null) throw new NotFoundException("User", request.Id);

        user.Name = request.Name;
        user.Email = request.Email;
        user.Role = request.Role;
        user.DepartmentId = request.DepartmentId;
        user.TelegramChatId = request.TelegramChatId;

        await _context.SaveChangesAsync(cancellationToken);

        // Keep the login identity's email/username aligned (no-op if none).
        await _accounts.UpdateEmailAsync(user.Id, request.Email, cancellationToken);
    }
}
