using CommunityToolkit.Mvvm.ComponentModel;
using MediaForge.Core.Configuration;

namespace MediaForge.App.ViewModels;

public sealed partial class MainPageViewModel : ObservableObject
{
    public MainPageViewModel(IApplicationPaths applicationPaths)
    {
        ArgumentNullException.ThrowIfNull(applicationPaths);
        IsPersistenceUnavailable = !applicationPaths.CanPersist;
    }

    public bool IsPersistenceUnavailable { get; }
}
