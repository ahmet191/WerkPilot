using MediatR;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Users.Commands;

public record CreateUserCommand(
    string Name,
    string Email,
    UserRole Role,
    Guid DepartmentId,
    string? TelegramChatId = null,
    string? InitialPassword = null,
    bool SendInvite = true) : IRequest<Guid>;
