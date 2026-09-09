using System.Globalization;
using System.Xml.Linq;
using MediaForge.Core.Configuration;

namespace MediaForge.App.Services;

/// <summary>Applies the persisted UI language and exposes resource lookup to code-behind pages.</summary>
public sealed class LocalizationService
{
    public const string SimplifiedChineseTag = "zh-Hans";
    public const string EnglishTag = "en-US";

    public event EventHandler? Changed;

    public ApplicationLanguage Language { get; private set; } = ApplicationLanguage.System;

    private IReadOnlyDictionary<string, string> _strings = new Dictionary<string, string>();

    public void Apply(ApplicationLanguage language)
    {
        Language = language;
        var languageTag = language switch
        {
            ApplicationLanguage.SimplifiedChinese => SimplifiedChineseTag,
            ApplicationLanguage.English => EnglishTag,
            _ => string.Empty
        };

        // ApplicationLanguages.PrimaryLanguageOverride is package-identity dependent and can
        // block indefinitely for an unpackaged process. Use managed culture settings here;
        // portable resource selection is handled independently by the application shell.
        var culture = string.IsNullOrEmpty(languageTag)
            ? CultureInfo.CurrentUICulture
            : CultureInfo.GetCultureInfo(languageTag);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        _strings = LoadStrings(IsChinese(culture) ? SimplifiedChineseTag : EnglishTag);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public string GetString(string key) =>
        _strings.TryGetValue(key, out var value) ? value : key;

    private static bool IsChinese(CultureInfo culture) =>
        culture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyDictionary<string, string> LoadStrings(string languageTag)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Strings", languageTag, "Resources.resw");
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            var document = XDocument.Load(path);
            return document.Root?.Elements("data")
                .Where(element => element.Attribute("name")?.Value is not null)
                .ToDictionary(
                    element => element.Attribute("name")!.Value,
                    element => element.Element("value")?.Value ?? string.Empty,
                    StringComparer.Ordinal)
                ?? new Dictionary<string, string>();
        }
        catch (System.Xml.XmlException)
        {
            return new Dictionary<string, string>();
        }
    }
}
