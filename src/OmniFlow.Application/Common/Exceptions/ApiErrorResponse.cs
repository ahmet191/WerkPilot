namespace OmniFlow.Application.Common.Exceptions;

public class ApiErrorResponse
{
    public string MessageKey { get; set; } = string.Empty;
    public object? Details { get; set; }
}
