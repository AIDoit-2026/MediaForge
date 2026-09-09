using MediaForge.Core.Configuration;
using Microsoft.UI.Xaml;
using PersistedTheme = MediaForge.Core.Configuration.ApplicationTheme;

namespace MediaForge.App.Services;

/// <summary>Applies the persisted requested theme to the active window content.</summary>
public sealed class ThemeService
{
    public PersistedTheme Theme { get; private set; } = PersistedTheme.System;

    public void Apply(PersistedTheme theme, FrameworkElement? root)
    {
        Theme = theme;
        if (root is not null)
        {
            root.RequestedTheme = theme switch
            {
                PersistedTheme.Light => ElementTheme.Light,
                PersistedTheme.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }
    }
}
