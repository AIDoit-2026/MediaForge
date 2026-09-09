using MediaForge.Core.Configuration;
using MediaForge.Core.Importing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
        PrimaryButtonText = strings.GetString("FolderImport.Import");
        CloseButtonText = strings.GetString("FolderImport.Cancel");
        DirectoryTextBox.Header = strings.GetString("FolderImport.Folder");
        DirectoryTextBox.PlaceholderText = strings.GetString("FolderImport.FolderPlaceholder");
        RecursiveCheckBox.Content = strings.GetString("FolderImport.Recursive");
        FilterModeComboBox.Header = strings.GetString("FolderImport.Filter");
        AllFilesOption.Content = strings.GetString("FolderImport.AllFiles");
        KeywordOption.Content = strings.GetString("FolderImport.Keyword");
        WildcardOption.Content = strings.GetString("FolderImport.Wildcard");
        RegularExpressionOption.Content = strings.GetString("FolderImport.RegularExpression");
        ExpressionTextBox.Header = strings.GetString("FolderImport.Expression");
    }

    private void OnFilterModeChanged(object sender, SelectionChangedEventArgs args) => UpdateExpressionState();

    private void UpdateExpressionState()
    {
        ExpressionTextBox.IsEnabled = FilterModeComboBox.SelectedIndex != (int)FileNameFilterMode.All;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var directory = DirectoryTextBox.Text.Trim();
        if (!Directory.Exists(directory))
        {
            args.Cancel = true;
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
            args.Cancel = true;
            ShowError(error.Message);
            return;
        }

        Result = new FolderImportSettings(
            directory,
            RecursiveCheckBox.IsChecked == true,
            mode,
            ExpressionTextBox.Text.Trim());
    }

    private void ShowError(string message)
    {
        ValidationInfoBar.Message = message;
        ValidationInfoBar.IsOpen = true;
    }
}
