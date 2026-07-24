namespace OmniFlow.Domain.Common;

public class MultiLingualString
{
    // Dictionary mapping Language Code (e.g., "en", "tr", "de") to the localized string
    public Dictionary<string, string> Translations { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public MultiLingualString() { }

    public MultiLingualString(string defaultLanguage, string defaultValue)
    {
        Translations[defaultLanguage] = defaultValue;
    }

    public string GetValueOrDefault(string languageCode, string defaultLanguage = "en")
    {
        if (Translations.TryGetValue(languageCode, out var value))
        {
            return value;
        }

        if (Translations.TryGetValue(defaultLanguage, out var defaultVal))
        {
            return defaultVal;
        }

        // Fallback to the first available translation or an empty string
        return Translations.Values.FirstOrDefault() ?? string.Empty;
    }
}
