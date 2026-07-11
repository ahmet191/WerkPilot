namespace OmniFlow.Application.Interfaces;

public interface IUserContext
{
    Guid UserId { get; }
    string Role { get; }
    string Language { get; }
}
