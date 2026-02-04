using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using WpfClient.ViewModels;

namespace WpfClient;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainViewModel();
        viewModel.ErrorOccurred += message => MessageBox.Show(this, message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        DataContext = viewModel;
    }

    private static readonly Regex PriceRegex = new(@"^\d*([.,]\d{0,2})?$");

    private void PriceTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        var proposed = GetProposedText(textBox, e.Text);
        e.Handled = !PriceRegex.IsMatch(proposed);
    }

    private void PriceTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        var pasteText = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        var proposed = GetProposedText(textBox, pasteText);
        if (!PriceRegex.IsMatch(proposed))
        {
            e.CancelCommand();
        }
    }

    private static string GetProposedText(TextBox textBox, string newText)
    {
        var text = textBox.Text;
        var selectionStart = textBox.SelectionStart;
        var selectionLength = textBox.SelectionLength;
        return text.Remove(selectionStart, selectionLength).Insert(selectionStart, newText);
    }

    private async void ItemsGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        e.Handled = true;
        var sortMember = e.Column.SortMemberPath;
        if (string.IsNullOrWhiteSpace(sortMember))
        {
            return;
        }

        var nextDirection = e.Column.SortDirection != ListSortDirection.Ascending
            ? ListSortDirection.Ascending
            : ListSortDirection.Descending;

        foreach (var column in ItemsGrid.Columns)
        {
            if (!ReferenceEquals(column, e.Column))
            {
                column.SortDirection = null;
            }
        }

        e.Column.SortDirection = nextDirection;
        var sortBy = sortMember.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase)
            ? "createdAt"
            : sortMember.ToLowerInvariant();
        var sortDir = nextDirection == ListSortDirection.Ascending ? "asc" : "desc";
        await viewModel.ApplySortAsync(sortBy, sortDir);
    }

    private void LoginPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (sender is PasswordBox passwordBox)
        {
            viewModel.LoginPassword = passwordBox.Password;
        }
    }
}