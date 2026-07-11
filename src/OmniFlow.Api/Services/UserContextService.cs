using OmniFlow.Application.Interfaces;

namespace OmniFlow.Api.Services;

public class UserContextService : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// The authenticated user's id from the JWT subject claim,
    /// or <see cref="Guid.Empty"/> ("system") outside an authenticated request.
    /// </summary>
    public Guid UserId
    {
        get
        {
            var claimValue = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

            return Guid.TryParse(claimValue, out var userId) ? userId : Guid.Empty;
        }
    }

    public string Role
    {
        get
        {
            var roleClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                         ?? _httpContextAccessor.HttpContext?.User?.FindFirst("role")?.Value;

            return roleClaim ?? string.Empty;
        }
    }

    public string Language
    {
        get
        {
            // Accept-Language typically looks like "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7"
            var acceptLanguage = _httpContextAccessor.HttpContext?.Request.Headers["Accept-Language"].FirstOrDefault();

            if (!string.IsNullOrEmpty(acceptLanguage))
            {
                var firstLang = acceptLanguage.Split(',').FirstOrDefault()?.Trim().Split('-').FirstOrDefault()?.ToLowerInvariant();
                if (!string.IsNullOrEmpty(firstLang))
                {
                    return firstLang;
                }
            }

            return "en";
        }
    }
}
