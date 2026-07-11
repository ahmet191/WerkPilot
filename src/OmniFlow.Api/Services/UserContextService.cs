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
            // For now, extract from headers or claims. Assuming a simplistic approach for testing
            // We will default to the "Requester" test user if no header is present
            var userIdStr = _httpContextAccessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault();
            if (Guid.TryParse(userIdStr, out var userId))
            {
                return userId;
            }
            return Guid.Parse("11111111-1111-1111-1111-111111111111"); // Fallback to Alice Requester
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
