namespace OmniFlow.Application.Interfaces;

public interface IUserContext
{
    Guid UserId { get; }
    string Role { get; }
    /// <summary>All role claims on the current principal (a user can hold several).</summary>
    IReadOnlyCollection<string> Roles { get; }
    string Language { get; }
}
