using OmniFlow.Application.Interfaces;

namespace OmniFlow.Api.Services;

public class UserContextService : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            // First try to get from JWT claims (sub)
            var claimValue = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
            
            if (Guid.TryParse(claimValue, out var claimUserId))
            {
                return claimUserId;
            }

            // Fallback to headers for legacy support
            var userIdStr = _httpContextAccessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault();
            if (Guid.TryParse(userIdStr, out var userId))
            {
                return userId;
            }
            return Guid.Parse("11111111-1111-1111-1111-111111111111"); // Fallback to Alice Requester
        }
    }

    public string Role
    {
        get
        {
            // Try to get from JWT claims
            var roleClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                         ?? _httpContextAccessor.HttpContext?.User?.FindFirst("role")?.Value;
            
            if (!string.IsNullOrEmpty(roleClaim))
            {
                return roleClaim;
            }

            // Fallback to headers
            var roleStr = _httpContextAccessor.HttpContext?.Request.Headers["X-User-Role"].FirstOrDefault();
            return string.IsNullOrEmpty(roleStr) ? "Admin" : roleStr;
        }
    }

    public string Language
    {
        get
        {
            // Extract preferred language from Accept-Language header
            // Accept-Language typically looks like "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7"
            var acceptLanguage = _httpContextAccessor.HttpContext?.Request.Headers["Accept-Language"].FirstOrDefault();
            
            if (!string.IsNullOrEmpty(acceptLanguage))
            {
                // Simple parsing to get the first 2-letter language code
                var firstLang = acceptLanguage.Split(',').FirstOrDefault()?.Trim().Split('-').FirstOrDefault()?.ToLowerInvariant();
                if (!string.IsNullOrEmpty(firstLang))
                {
                    return firstLang; // Returns "tr", "en", "de", etc.
                }
            }

            return "en"; // Fallback default language
        }
    }
}
