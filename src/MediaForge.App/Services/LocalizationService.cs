using System.Globalization;
using MediaForge.Core.Configuration;

namespace MediaForge.App.Services;

/// <summary>Applies the persisted UI language and exposes resource lookup to code-behind pages.</summary>
public sealed class LocalizationService
{
    public const string SimplifiedChineseTag = "zh-Hans";
    public const string EnglishTag = "en-US";

    public event EventHandler? Changed;

    public ApplicationLanguage Language { get; private set; } = ApplicationLanguage.System;

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
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
