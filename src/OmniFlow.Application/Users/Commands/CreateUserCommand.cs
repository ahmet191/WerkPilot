using MediatR;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Users.Commands;

public record CreateUserCommand(
    string Name,
    string Email,
    UserRole Role,
    Guid DepartmentId,
    string? TelegramChatId = null) : IRequest<Guid>;
