namespace OmniFlow.Application.Interfaces;

public interface IUserContext
{
    Guid UserId { get; }
    string Language { get; }
}
