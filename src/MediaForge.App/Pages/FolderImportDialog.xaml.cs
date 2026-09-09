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
        Title = "Import folder";
        PrimaryButtonText = "Import";
        CloseButtonText = "Cancel";
        DirectoryTextBox.Text = settings.Directory ?? string.Empty;
        RecursiveCheckBox.IsChecked = settings.IncludeSubdirectories;
        FilterModeComboBox.SelectedIndex = (int)settings.FilterMode;
        ExpressionTextBox.Text = settings.FilterExpression;
        UpdateExpressionState();
    }

    public FolderImportSettings? Result { get; private set; }

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
            ShowError("Choose an existing folder.");
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
