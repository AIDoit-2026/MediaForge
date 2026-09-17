using MediaForge.Core.Configuration;
using MediaForge.Core.Importing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace MediaForge.App.Pages;

public sealed partial class FolderImportDialog : ContentDialog
{
    public FolderImportDialog(FolderImportSettings settings)
    {
        InitializeComponent();
        ApplyStrings();
        DirectoryTextBox.Text = settings.Directory ?? string.Empty;
        RecursiveCheckBox.IsChecked = settings.IncludeSubdirectories;
        FilterModeComboBox.SelectedIndex = (int)settings.FilterMode;
        ExpressionTextBox.Text = settings.FilterExpression;
        UpdateExpressionState();
    }

    public FolderImportSettings? Result { get; private set; }

    private void ApplyStrings()
    {
        var strings = App.Services.Localization;
        Title = strings.GetString("FolderImport.Title");
        ImportButton.Content = strings.GetString("FolderImport.Import");
        CancelButton.Content = strings.GetString("FolderImport.Cancel");
        DirectoryTextBox.Header = strings.GetString("FolderImport.Folder");
        DirectoryTextBox.PlaceholderText = strings.GetString("FolderImport.FolderPlaceholder");
        BrowseDirectoryButton.SetValue(Microsoft.UI.Xaml.Automation.AutomationProperties.NameProperty, "Browse folder");
        RecursiveCheckBox.Content = strings.GetString("FolderImport.Recursive");
        FilterModeComboBox.Header = strings.GetString("FolderImport.Filter");
        AllFilesOption.Content = strings.GetString("FolderImport.AllFiles");
        KeywordOption.Content = strings.GetString("FolderImport.Keyword");
        WildcardOption.Content = strings.GetString("FolderImport.Wildcard");
        RegularExpressionOption.Content = strings.GetString("FolderImport.RegularExpression");
        ExpressionTextBox.Header = strings.GetString("FolderImport.Expression");
    }

    private void OnFilterModeChanged(object sender, SelectionChangedEventArgs args) => UpdateExpressionState();

    private async void OnBrowseDirectoryClick(object sender, RoutedEventArgs args)
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");
        WinRT.Interop.InitializeWithWindow.Initialize(picker, App.WindowHandle);
        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null) DirectoryTextBox.Text = folder.Path;
    }

    private void UpdateExpressionState()
    {
        ExpressionTextBox.IsEnabled = FilterModeComboBox.SelectedIndex != (int)FileNameFilterMode.All;
    }

    private void OnImportClick(object sender, RoutedEventArgs args)
    {
        var directory = DirectoryTextBox.Text.Trim();
        if (!Directory.Exists(directory))
        {
            ShowError(App.Services.Localization.GetString("FolderImport.DirectoryRequired"));
            return;
        }

        var mode = (FileNameFilterMode)FilterModeComboBox.SelectedIndex;
        try
        {
            _ = FileNameFilter.Create(mode, ExpressionTextBox.Text);
        }
        catch (ArgumentException error)
        {
            ShowError(error.Message);
            return;
        }

        Result = new FolderImportSettings(
            directory,
            RecursiveCheckBox.IsChecked == true,
            mode,
            ExpressionTextBox.Text.Trim());
        Hide();
    }

    private void OnCancelClick(object sender, RoutedEventArgs args) => Hide();

    private void ShowError(string message)
    {
        ValidationInfoBar.Message = message;
        ValidationInfoBar.IsOpen = true;
    }
}
