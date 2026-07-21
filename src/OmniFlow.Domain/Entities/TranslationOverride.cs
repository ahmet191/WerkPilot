using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

/// <summary>
/// A tenant's customization of one UI string. The platform ships a built-in
/// dictionary as the default; only the strings a tenant actually changes are
/// stored here, so new keys added in code keep working and no bulk migration
/// is needed.
/// </summary>
public class TranslationOverride : AuditableEntity
{
    /// <summary>Two-letter language code ("en", "tr").</summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>Translation key, e.g. "sidebar.tickets".</summary>
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
