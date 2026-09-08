using CommunityToolkit.Mvvm.ComponentModel;
using MediaForge.Core.Configuration;

namespace MediaForge.App.ViewModels;

public sealed partial class MainPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool IsPersistenceUnavailable { get; private set; }

    public void Initialize(IApplicationPaths applicationPaths)
    {
        ArgumentNullException.ThrowIfNull(applicationPaths);
        IsPersistenceUnavailable = !applicationPaths.CanPersist;
    }
}
