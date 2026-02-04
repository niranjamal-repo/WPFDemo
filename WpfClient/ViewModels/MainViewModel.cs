using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Shared;
using WpfClient.Services;
using WpfClient.Utilities;

namespace WpfClient.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IDataErrorInfo
{
    private readonly ItemApiClient _apiClient;
    private Item? _selectedItem;
    private string _searchText = string.Empty;
    private string _sortBy = "createdAt";
    private string _sortDir = "desc";
    private string _editName = string.Empty;
    private string _editDescription = string.Empty;
    private decimal _editPrice;
    private string _statusMessage = "Ready";
    private string _loginUserName = string.Empty;
    private string _loginPassword = string.Empty;
    private bool _isAuthenticated;
    private string _authStatus = "Not signed in";
    private int _page = 1;
    private int _pageSize = 20;
    private int _totalCount;
    private bool _isLoading;
    private bool _suppressSortLoad;
    private bool _suppressPageLoad;

    public MainViewModel()
    {
        _apiClient = new ItemApiClient("https://localhost:7042/");

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        CreateCommand = new AsyncRelayCommand(CreateAsync, () => !string.IsNullOrWhiteSpace(EditName) && IsAuthenticated);
        UpdateCommand = new AsyncRelayCommand(UpdateAsync, () => SelectedItem is not null && IsAuthenticated);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => SelectedItem is not null && IsAuthenticated);
        NewCommand = new RelayCommand(ClearEditor);
        SearchCommand = new AsyncRelayCommand(SearchAsync);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => Page < TotalPages);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => Page > 1);
        LoginCommand = new AsyncRelayCommand(LoginAsync, () =>
            !IsAuthenticated &&
            !string.IsNullOrWhiteSpace(LoginUserName) &&
            !string.IsNullOrWhiteSpace(LoginPassword));
        LogoutCommand = new RelayCommand(Logout, () => IsAuthenticated);

        _ = LoadAsync();
    }

    public ObservableCollection<Item> Items { get; } = new();

    public Item? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetField(ref _selectedItem, value))
            {
                if (value is not null)
                {
                    EditName = value.Name;
                    EditDescription = value.Description;
                    EditPrice = value.Price;
                }
                else
                {
                    ClearEditorFields();
                }

                RaiseCommandCanExecuteChanged();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set => SetField(ref _searchText, value);
    }

    public string SortBy
    {
        get => _sortBy;
        set
        {
            if (SetField(ref _sortBy, value) && !_suppressSortLoad)
            {
                _ = LoadAsync();
            }
        }
    }

    public string SortDirection
    {
        get => _sortDir;
        set
        {
            if (SetField(ref _sortDir, value) && !_suppressSortLoad)
            {
                _ = LoadAsync();
            }
        }
    }

    public string EditName
    {
        get => _editName;
        set
        {
            if (SetField(ref _editName, value))
            {
                RaiseCommandCanExecuteChanged();
            }
        }
    }

    public string EditDescription
    {
        get => _editDescription;
        set => SetField(ref _editDescription, value);
    }

    public decimal EditPrice
    {
        get => _editPrice;
        set => SetField(ref _editPrice, value);
    }

    public string LoginUserName
    {
        get => _loginUserName;
        set
        {
            if (SetField(ref _loginUserName, value))
            {
                RaiseAuthCommandCanExecuteChanged();
            }
        }
    }

    public string LoginPassword
    {
        get => _loginPassword;
        set
        {
            if (SetField(ref _loginPassword, value))
            {
                RaiseAuthCommandCanExecuteChanged();
            }
        }
    }

    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        set
        {
            if (SetField(ref _isAuthenticated, value))
            {
                AuthStatus = value
                    ? $"Signed in as {LoginUserName}"
                    : "Not signed in";
                RaiseCommandCanExecuteChanged();
                RaiseAuthCommandCanExecuteChanged();
            }
        }
    }

    public string AuthStatus
    {
        get => _authStatus;
        set => SetField(ref _authStatus, value);
    }

    public int Page
    {
        get => _page;
        set
        {
            if (SetField(ref _page, value))
            {
                RaisePagingCanExecuteChanged();
            }
        }
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (SetField(ref _pageSize, value) && !_suppressPageLoad)
            {
                Page = 1;
                RaisePagingCanExecuteChanged();
                _ = LoadAsync();
            }
        }
    }

    public int TotalCount
    {
        get => _totalCount;
        set
        {
            if (SetField(ref _totalCount, value))
            {
                OnPropertyChanged(nameof(TotalPages));
                RaisePagingCanExecuteChanged();
            }
        }
    }

    public int TotalPages => PageSize == 0 ? 1 : (int)Math.Ceiling((double)TotalCount / PageSize);

    public bool IsLoading
    {
        get => _isLoading;
        set => SetField(ref _isLoading, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public AsyncRelayCommand LoadCommand { get; }
    public AsyncRelayCommand CreateCommand { get; }
    public AsyncRelayCommand UpdateCommand { get; }
    public AsyncRelayCommand DeleteCommand { get; }
    public RelayCommand NewCommand { get; }
    public AsyncRelayCommand SearchCommand { get; }
    public AsyncRelayCommand NextPageCommand { get; }
    public AsyncRelayCommand PreviousPageCommand { get; }
    public AsyncRelayCommand LoginCommand { get; }
    public RelayCommand LogoutCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<string>? ErrorOccurred;

    public string Error => string.Empty;

    public string this[string columnName]
    {
        get
        {
            return columnName switch
            {
                nameof(EditName) when string.IsNullOrWhiteSpace(EditName) => "Name is required.",
                nameof(EditPrice) when EditPrice < 0 => "Price must be 0 or greater.",
                nameof(PageSize) when PageSize < 1 || PageSize > 100 => "Page size must be between 1 and 100.",
                nameof(LoginUserName) when string.IsNullOrWhiteSpace(LoginUserName) => "Username is required.",
                nameof(LoginPassword) when string.IsNullOrWhiteSpace(LoginPassword) => "Password is required.",
                _ => string.Empty
            };
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading items...";
            var result = await _apiClient.GetItemsAsync(SearchText, SortBy, SortDirection, Page, PageSize);
            TotalCount = result.TotalCount;

            if (TotalPages > 0 && Page > TotalPages)
            {
                Page = TotalPages;
                result = await _apiClient.GetItemsAsync(SearchText, SortBy, SortDirection, Page, PageSize);
                TotalCount = result.TotalCount;
            }

            Items.Clear();
            foreach (var item in result.Items)
            {
                Items.Add(item);
            }

            _suppressPageLoad = true;
            Page = result.Page;
            PageSize = result.PageSize;
            _suppressPageLoad = false;

            StatusMessage = $"Loaded {Items.Count} items.";
        }
        catch (Exception ex)
        {
            NotifyError($"Load failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CreateAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Creating item...";
            var created = await _apiClient.CreateAsync(new Item
            {
                Name = EditName.Trim(),
                Description = EditDescription.Trim(),
                Price = EditPrice
            });

            Page = 1;
            await LoadAsync();
            SelectedItem = Items.FirstOrDefault(item => item.Id == created.Id);
            StatusMessage = "Item created.";
        }
        catch (Exception ex)
        {
            NotifyError($"Create failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task UpdateAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Updating item...";
            var updated = new Item
            {
                Id = SelectedItem.Id,
                Name = EditName.Trim(),
                Description = EditDescription.Trim(),
                Price = EditPrice,
                CreatedAt = SelectedItem.CreatedAt
            };

            await _apiClient.UpdateAsync(SelectedItem.Id, updated);
            await LoadAsync();
            SelectedItem = Items.FirstOrDefault(item => item.Id == updated.Id);
            StatusMessage = "Item updated.";
        }
        catch (Exception ex)
        {
            NotifyError($"Update failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Deleting item...";
            var id = SelectedItem.Id;
            await _apiClient.DeleteAsync(id);
            await LoadAsync();
            StatusMessage = "Item deleted.";
        }
        catch (Exception ex)
        {
            NotifyError($"Delete failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ApplySortAsync(string sortBy, string sortDir)
    {
        _suppressSortLoad = true;
        SortBy = sortBy;
        SortDirection = sortDir;
        _suppressSortLoad = false;
        await LoadAsync();
    }

    private async Task SearchAsync()
    {
        Page = 1;
        await LoadAsync();
    }

    private async Task NextPageAsync()
    {
        if (Page < TotalPages)
        {
            Page += 1;
            await LoadAsync();
        }
    }

    private async Task PreviousPageAsync()
    {
        if (Page > 1)
        {
            Page -= 1;
            await LoadAsync();
        }
    }

    private async Task LoginAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Signing in...";
            var token = await _apiClient.RequestTokenAsync(new WpfClient.Models.TokenRequest
            {
                UserName = LoginUserName.Trim(),
                Password = LoginPassword
            });

            _apiClient.SetAccessToken(token.AccessToken);
            IsAuthenticated = true;
            StatusMessage = "Signed in.";
        }
        catch (Exception ex)
        {
            IsAuthenticated = false;
            NotifyError($"Sign-in failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Logout()
    {
        _apiClient.SetAccessToken(null);
        IsAuthenticated = false;
        StatusMessage = "Signed out.";
    }

    private void ClearEditor()
    {
        SelectedItem = null;
        ClearEditorFields();
        StatusMessage = "Editor cleared.";
    }

    private void ClearEditorFields()
    {
        EditName = string.Empty;
        EditDescription = string.Empty;
        EditPrice = 0m;
    }

    private void RaiseCommandCanExecuteChanged()
    {
        CreateCommand.RaiseCanExecuteChanged();
        UpdateCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
    }

    private void RaiseAuthCommandCanExecuteChanged()
    {
        LoginCommand.RaiseCanExecuteChanged();
        LogoutCommand.RaiseCanExecuteChanged();
    }

    private void RaisePagingCanExecuteChanged()
    {
        NextPageCommand.RaiseCanExecuteChanged();
        PreviousPageCommand.RaiseCanExecuteChanged();
    }

    private void NotifyError(string message)
    {
        StatusMessage = message;
        ErrorOccurred?.Invoke(message);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
