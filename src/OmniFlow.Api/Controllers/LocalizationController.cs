using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Api.Localization;
using OmniFlow.Application.Interfaces;
using OmniFlow.Infrastructure.Authorization;

namespace OmniFlow.Api.Controllers;

/// <summary>
/// Serves UI strings: the shipped defaults merged with whatever the caller's
/// tenant has customized. Reading is anonymous because the login page needs
/// translations before sign-in — an anonymous caller has no tenant and so
/// simply receives the defaults. Editing requires <c>translations.manage</c>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LocalizationController : ControllerBase
{
    private readonly ITranslationService _translations;

    public LocalizationController(ITranslationService translations)
    {
        _translations = translations;
    }

    [HttpGet("{lang}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTranslations(string lang)
    {
        var language = BuiltInTranslations.Normalize(lang);
        var merged = new Dictionary<string, string>(BuiltInTranslations.For(language), StringComparer.Ordinal);

        foreach (var (key, value) in await _translations.GetOverridesAsync(language, HttpContext.RequestAborted))
        {
            merged[key] = value;
        }

        return Ok(merged);
    }

    /// <summary>Every key with its shipped default and the tenant's override, for the editor.</summary>
    [HttpGet("admin/{lang}")]
    [Authorize(Policy = PermissionPolicyNames.TranslationsManage)]
    public async Task<IActionResult> GetForEditing(string lang)
    {
        var language = BuiltInTranslations.Normalize(lang);
        var defaults = BuiltInTranslations.For(language);
        var overrides = await _translations.GetOverridesAsync(language, HttpContext.RequestAborted);

        var items = defaults
            .Select(kv => new
            {
                key = kv.Key,
                defaultValue = kv.Value,
                overrideValue = overrides.TryGetValue(kv.Key, out var v) ? v : null,
                isCustomized = overrides.ContainsKey(kv.Key),
            })
            .OrderBy(i => i.key, StringComparer.Ordinal)
            .ToList();

        return Ok(new { language, languages = BuiltInTranslations.Languages, items });
    }

    public record SetTranslationRequest([Required] string Value);

    [HttpPut("admin/{lang}/{key}")]
    [Authorize(Policy = PermissionPolicyNames.TranslationsManage)]
    public async Task<IActionResult> SetOverride(string lang, string key, [FromBody] SetTranslationRequest request)
    {
        await _translations.SetAsync(
            BuiltInTranslations.Normalize(lang), key, request.Value, HttpContext.RequestAborted);
        return NoContent();
    }

    /// <summary>Drops the customization, reverting the key to the shipped default.</summary>
    [HttpDelete("admin/{lang}/{key}")]
    [Authorize(Policy = PermissionPolicyNames.TranslationsManage)]
    public async Task<IActionResult> RemoveOverride(string lang, string key)
    {
        await _translations.RemoveAsync(
            BuiltInTranslations.Normalize(lang), key, HttpContext.RequestAborted);
        return NoContent();
    }
}
