using MediatR;
using OmniFlow.Domain.Enums;
using OmniFlow.Application.Common.Exceptions;
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

    public UpdateUserCommandHandler(IOmniFlowDbContext context)
    {
        _context = context;
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
    }
}
