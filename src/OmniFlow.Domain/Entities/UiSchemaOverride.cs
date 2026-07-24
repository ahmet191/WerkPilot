using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

/// <summary>
/// A tenant's customization of one entity's UI schema (grid columns, form
/// fields, titles). The platform ships built-in schemas as the default; only
/// what a tenant changes is stored here as a JSON document, so schemas added or
/// changed in code keep working and no bulk migration is needed. Security-
/// sensitive parts (endpoints, role-gated row actions) are never taken from the
/// override — they are always recomputed from the built-in at request time.
/// </summary>
public class UiSchemaOverride : AuditableEntity
{
    /// <summary>The entity type this schema is for, e.g. "tickets", "assets".</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>The serialized UiSchema the tenant customized to.</summary>
    public string SchemaJson { get; set; } = string.Empty;
}
