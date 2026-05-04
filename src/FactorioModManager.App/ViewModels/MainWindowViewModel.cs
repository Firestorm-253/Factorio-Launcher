using System.Collections.ObjectModel;
using System.ComponentModel;
using FactorioModManager.App.Factorio;
using FactorioModManager.App.Models;
using FactorioModManager.App.Services;

namespace FactorioModManager.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IDialogService _dialogService;
    private readonly AppSettingsService _appSettingsService;
    private readonly FolderValidator _folderValidator;
    private readonly ModScanner _modScanner;
    private readonly ModListDetector _modListDetector;
    private readonly ModListWriter _modListWriter;
    private readonly ModListMetadataService _metadataService;
    private readonly ModSettingsManager _modSettingsManager;
    private readonly ModListActivator _modListActivator;
    private readonly ModListFileManager _modListFileManager;
    private readonly NameValidator _nameValidator;
    private readonly ActiveModListDetector _activeModListDetector;
    private readonly List<ModInfo> _availableMods = [];
    private readonly List<EditableModViewModel> _allEditableMods = [];
    private readonly List<ModListItemViewModel> _allModListItems = [];
    private AppSettings _settings = new();

    private string? _modsFolderPath;
    private string _folderStatus = "Select your Factorio mods folder to begin.";
    private bool _isFolderValid;
    private string _statusMessage = "Ready.";
    private string? _errorMessage;
    private ModListItemViewModel? _selectedModList;
    private bool _isEditMode;
    private bool _isCreating;
    private string? _searchText;
    private string? _listSearchText;
    private string _activeTab = "Lists";
    private string _editorSortMode = EditorSortModes.Active;
    private string _draftName = string.Empty;
    private string _draftDescription = string.Empty;
    private int _draftSelectedCount;
    private string _draftSizeLabel = "-";
    private string? _activeListName;

    public MainWindowViewModel(
        IDialogService dialogService,
        AppSettingsService appSettingsService,
        FolderValidator folderValidator,
        ModScanner modScanner,
        ModListDetector modListDetector,
        ModListWriter modListWriter,
        ModListMetadataService modListMetadataService,
        ModSettingsManager modSettingsManager,
        BackupService backupService,
        ModListActivator modListActivator,
        ModListFileManager modListFileManager,
        NameValidator nameValidator,
        ActiveModListDetector activeModListDetector)
    {
        _dialogService = dialogService;
        _appSettingsService = appSettingsService;
        _folderValidator = folderValidator;
        _modScanner = modScanner;
        _modListDetector = modListDetector;
        _modListWriter = modListWriter;
        _metadataService = modListMetadataService;
        _modSettingsManager = modSettingsManager;
        _modListActivator = modListActivator;
        _modListFileManager = modListFileManager;
        _nameValidator = nameValidator;
        _activeModListDetector = activeModListDetector;

        BrowseFolderCommand = new AsyncRelayCommand(BrowseFolderAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => IsFolderValid);
        CreateModListCommand = new RelayCommand(StartCreateDraft, () => IsFolderValid && IsNormalMode);
        SaveEditCommand = new AsyncRelayCommand(SaveEditAsync, () => IsEditMode);
        CancelEditCommand = new RelayCommand(CancelEdit, () => IsEditMode);
        EditSelectedCommand = new RelayCommand(EditSelected, () => SelectedModList is not null && IsNormalMode);
        ActivateSelectedCommand = new AsyncRelayCommand(ActivateSelectedAsync, () => CanActivateSelected);
        DeleteSelectedCommand = new AsyncRelayCommand(DeleteSelectedAsync, () => SelectedModList is not null && IsNormalMode);
        ShowListsCommand = new RelayCommand(() => ActiveTab = "Lists");
        ShowModsCommand = new RelayCommand(() => ActiveTab = "Mods");
        SortEditorByNameCommand = new RelayCommand(() => EditorSortMode = EditorSortModes.Name);
        SortEditorByActiveCommand = new RelayCommand(() => EditorSortMode = EditorSortModes.Active);
    }

    public string? ModsFolderPath
    {
        get => _modsFolderPath;
        private set
        {
            if (SetProperty(ref _modsFolderPath, value))
            {
                OnPropertyChanged(nameof(FolderPathDisplay));
                OnPropertyChanged(nameof(FolderPathCompact));
            }
        }
    }

    public string FolderPathDisplay => string.IsNullOrWhiteSpace(ModsFolderPath)
        ? "No folder selected"
        : ModsFolderPath;

    public string FolderPathCompact
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ModsFolderPath))
            {
                return "no folder";
            }

            var parts = ModsFolderPath
                .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)
                .TakeLast(3);
            return string.Join("/", parts);
        }
    }

    public string FolderStatus
    {
        get => _folderStatus;
        private set => SetProperty(ref _folderStatus, value);
    }

    public bool IsFolderValid
    {
        get => _isFolderValid;
        private set
        {
            if (SetProperty(ref _isFolderValid, value))
            {
                OnPropertyChanged(nameof(CanUseWorkspace));
                RefreshCommand.RaiseCanExecuteChanged();
                CreateModListCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanUseWorkspace => IsFolderValid;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public ObservableCollection<ModListItemViewModel> ModLists { get; } = [];
    public ObservableCollection<DisplayModViewModel> SelectedMods { get; } = [];
    public ObservableCollection<EditableModViewModel> EditableMods { get; } = [];
    public ObservableCollection<InstalledModViewModel> InstalledMods { get; } = [];

    public ModListItemViewModel? SelectedModList
    {
        get => _selectedModList;
        set
        {
            if (SetProperty(ref _selectedModList, value))
            {
                OnPropertyChanged(nameof(HasSelectedModList));
                OnPropertyChanged(nameof(SelectedListName));
                OnPropertyChanged(nameof(SelectedListDescription));
                OnPropertyChanged(nameof(SelectedListSummary));
                OnPropertyChanged(nameof(CanActivateSelected));
                RaiseSelectionCommandStates();
                if (!IsEditMode)
                {
                    LoadSelectedMods();
                }
            }
        }
    }

    public bool HasSelectedModList => SelectedModList is not null;
    public string SelectedListName => SelectedModList?.Name ?? "No mod list selected";
    public string SelectedListDescription => SelectedModList?.Description ?? string.Empty;
    public string SelectedListSummary => SelectedModList?.SummaryLabel ?? "Select or create a mod list.";

    public bool IsEditMode
    {
        get => _isEditMode;
        private set
        {
            if (SetProperty(ref _isEditMode, value))
            {
                OnPropertyChanged(nameof(IsNormalMode));
                OnPropertyChanged(nameof(IsEditorVisible));
                OnPropertyChanged(nameof(CanActivateSelected));
                SaveEditCommand.RaiseCanExecuteChanged();
                CancelEditCommand.RaiseCanExecuteChanged();
                CreateModListCommand.RaiseCanExecuteChanged();
                RaiseSelectionCommandStates();
            }
        }
    }

    public bool IsNormalMode => !IsEditMode;
    public bool IsEditorVisible => IsEditMode;

    public string? SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyEditorFilter();
            }
        }
    }

    public string? ListSearchText
    {
        get => _listSearchText;
        set
        {
            if (SetProperty(ref _listSearchText, value))
            {
                ApplyListFilter();
            }
        }
    }

    public string ActiveTab
    {
        get => _activeTab;
        private set
        {
            if (SetProperty(ref _activeTab, value))
            {
                OnPropertyChanged(nameof(IsListsTabActive));
                OnPropertyChanged(nameof(IsModsTabActive));
                OnPropertyChanged(nameof(IsListsTabInactive));
                OnPropertyChanged(nameof(IsModsTabInactive));
            }
        }
    }

    public bool IsListsTabActive => string.Equals(ActiveTab, "Lists", StringComparison.Ordinal);
    public bool IsModsTabActive => string.Equals(ActiveTab, "Mods", StringComparison.Ordinal);
    public bool IsListsTabInactive => !IsListsTabActive;
    public bool IsModsTabInactive => !IsModsTabActive;

    public string EditorSortMode
    {
        get => _editorSortMode;
        private set
        {
            if (SetProperty(ref _editorSortMode, value))
            {
                OnPropertyChanged(nameof(IsEditorSortedByName));
                OnPropertyChanged(nameof(IsEditorSortedByActive));
                OnPropertyChanged(nameof(IsEditorNotSortedByName));
                OnPropertyChanged(nameof(IsEditorNotSortedByActive));
                ApplyEditorFilter();
            }
        }
    }

    public bool IsEditorSortedByName => string.Equals(EditorSortMode, EditorSortModes.Name, StringComparison.Ordinal);
    public bool IsEditorSortedByActive => string.Equals(EditorSortMode, EditorSortModes.Active, StringComparison.Ordinal);
    public bool IsEditorNotSortedByName => !IsEditorSortedByName;
    public bool IsEditorNotSortedByActive => !IsEditorSortedByActive;

    public string DraftName
    {
        get => _draftName;
        set => SetProperty(ref _draftName, value);
    }

    public string DraftDescription
    {
        get => _draftDescription;
        set => SetProperty(ref _draftDescription, value);
    }

    public int DraftSelectedCount
    {
        get => _draftSelectedCount;
        private set
        {
            if (SetProperty(ref _draftSelectedCount, value))
            {
                OnPropertyChanged(nameof(DraftSummaryLabel));
            }
        }
    }

    public string DraftSizeLabel
    {
        get => _draftSizeLabel;
        private set
        {
            if (SetProperty(ref _draftSizeLabel, value))
            {
                OnPropertyChanged(nameof(DraftSummaryLabel));
            }
        }
    }

    public string DraftSummaryLabel => $"{DraftSelectedCount} mods selected - {DraftSizeLabel}";

    public bool HasActiveList => !string.IsNullOrWhiteSpace(ActiveListName);
    public bool CanActivateSelected => SelectedModList is not null && !SelectedModList.IsActive && IsNormalMode;

    public string? ActiveListName
    {
        get => _activeListName;
        private set
        {
            if (SetProperty(ref _activeListName, value))
            {
                OnPropertyChanged(nameof(HasActiveList));
            }
        }
    }

    public int AvailableModCount => _availableMods.Count;
    public int ModListCount => _allModListItems.Count;
    public int VisibleModListCount => ModLists.Count;

    public AsyncRelayCommand BrowseFolderCommand { get; }
    public AsyncRelayCommand RefreshCommand { get; }
    public RelayCommand CreateModListCommand { get; }
    public AsyncRelayCommand SaveEditCommand { get; }
    public RelayCommand CancelEditCommand { get; }
    public RelayCommand EditSelectedCommand { get; }
    public AsyncRelayCommand ActivateSelectedCommand { get; }
    public AsyncRelayCommand DeleteSelectedCommand { get; }
    public RelayCommand ShowListsCommand { get; }
    public RelayCommand ShowModsCommand { get; }
    public RelayCommand SortEditorByNameCommand { get; }
    public RelayCommand SortEditorByActiveCommand { get; }

    public async Task InitializeAsync()
    {
        _settings = await _appSettingsService.LoadAsync();
        ModsFolderPath = _settings.LastModsFolderPath;
        ValidateFolder();

        if (IsFolderValid)
        {
            await RefreshAsync();
        }
    }

    private async Task BrowseFolderAsync()
    {
        var selected = await _dialogService.PickFolderAsync("Select Factorio mods folder");
        if (string.IsNullOrWhiteSpace(selected))
        {
            return;
        }

        ModsFolderPath = selected;
        if (!PathsEqual(_settings.LastModsFolderPath, selected))
        {
            _settings.ActiveModListFolderPath = null;
        }

        _settings.LastModsFolderPath = selected;
        await _appSettingsService.SaveAsync(_settings);
        ValidateFolder();
        await RefreshAsync();
    }

    private void ValidateFolder()
    {
        var result = _folderValidator.Validate(ModsFolderPath);
        IsFolderValid = result.IsValid;
        FolderStatus = result.Message;
        if (!result.IsValid)
        {
            _allModListItems.Clear();
            ModLists.Clear();
            SelectedMods.Clear();
            EditableMods.Clear();
            InstalledMods.Clear();
            _availableMods.Clear();
            ActiveListName = null;
        }
    }

    private async Task RefreshAsync()
    {
        ErrorMessage = null;
        ValidateFolder();
        if (!IsFolderValid || string.IsNullOrWhiteSpace(ModsFolderPath))
        {
            StatusMessage = FolderStatus;
            return;
        }

        try
        {
            var previousSelection = SelectedModList?.Name;
            _availableMods.Clear();
            _availableMods.AddRange(_modScanner.Scan(ModsFolderPath));
            LoadInstalledMods();

            var detectedModLists = _modListDetector.Detect(ModsFolderPath).ToList();
            var activeFolderPath = await GetValidatedRememberedActiveFolderPathAsync(detectedModLists);

            _allModListItems.Clear();
            foreach (var modList in detectedModLists)
            {
                var isActive = PathsEqual(activeFolderPath, modList.FolderPath);
                _allModListItems.Add(new ModListItemViewModel(
                    modList,
                    isActive,
                    CalculateSelectedSizeLabel(modList.SelectedMods),
                    ActivateAsync,
                    StartEdit,
                    RenameAsync,
                    DeleteAsync));
            }

            ActiveListName = _allModListItems.FirstOrDefault(item => item.IsActive)?.Name;
            ApplyListFilter();

            SelectedModList = _allModListItems.FirstOrDefault(item =>
                string.Equals(item.Name, previousSelection, StringComparison.OrdinalIgnoreCase)) ??
                _allModListItems.FirstOrDefault();

            if (!IsEditMode)
            {
                LoadSelectedMods();
            }

            OnPropertyChanged(nameof(AvailableModCount));
            OnPropertyChanged(nameof(ModListCount));
            StatusMessage = $"Loaded {_availableMods.Count} mods and {_allModListItems.Count} mod lists.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Refresh failed", ex.Message);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Refresh failed while reading mod data.";
            await _dialogService.ShowErrorAsync("Refresh failed", ex.Message);
        }
    }

    private void StartCreateDraft()
    {
        if (!EnsureWorkspace())
        {
            return;
        }

        _isCreating = true;
        SelectedModList = null;
        DraftName = GenerateUniqueDraftName();
        DraftDescription = string.Empty;
        BeginEdit([], new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        ActiveTab = "Lists";
        StatusMessage = "Creating a new mod list.";
    }

    private void EditSelected()
    {
        if (SelectedModList is not null)
        {
            StartEdit(SelectedModList);
        }
    }

    private void StartEdit(ModListItemViewModel item)
    {
        SelectedModList = item;
        _isCreating = false;
        DraftName = item.Name;
        DraftDescription = item.ModList.Description;
        BeginEdit(item.ModList.SelectedMods, item.ModList.SelectedVersions);
        ActiveTab = "Lists";
        StatusMessage = $"Editing {item.Name}.";
    }

    private void BeginEdit(IEnumerable<string> selectedModNames, IReadOnlyDictionary<string, string> selectedVersions)
    {
        var selected = selectedModNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        _allEditableMods.Clear();

        foreach (var mod in _availableMods)
        {
            selectedVersions.TryGetValue(mod.Name, out var selectedVersion);
            var editable = new EditableModViewModel(mod, selected.Contains(mod.Name), selectedVersion);
            editable.PropertyChanged += EditableModChanged;
            _allEditableMods.Add(editable);
        }

        SearchText = string.Empty;
        EditorSortMode = EditorSortModes.Active;
        IsEditMode = true;
        ApplyEditorFilter();
        UpdateDraftSummary();
    }

    private async Task SaveEditAsync()
    {
        if (!EnsureWorkspace())
        {
            return;
        }

        var selectedNames = _allEditableMods
            .Where(mod => mod.IsSelected)
            .Select(mod => mod.Name)
            .ToList();
        var availableNames = _availableMods.Select(mod => mod.Name).ToList();
        var selectedVersions = _allEditableMods
            .Where(mod => mod.IsSelected && !string.IsNullOrWhiteSpace(mod.SelectedVersion))
            .ToDictionary(mod => mod.Name, mod => mod.SelectedVersion, StringComparer.OrdinalIgnoreCase);

        if (_isCreating)
        {
            await SaveNewListAsync(selectedNames, availableNames, selectedVersions);
            return;
        }

        if (SelectedModList is null)
        {
            await _dialogService.ShowErrorAsync("Nothing selected", "Select a mod list before saving.");
            return;
        }

        var newName = DraftName.Trim();
        var validation = _nameValidator.Validate(newName, ModsFolderPath!, _allModListItems.Select(list => list.Name), SelectedModList.Name);
        if (!validation.IsValid)
        {
            await _dialogService.ShowErrorAsync("Invalid name", validation.Message);
            return;
        }

        try
        {
            var targetFolder = SelectedModList.FolderPath;
            if (!string.Equals(newName, SelectedModList.Name, StringComparison.OrdinalIgnoreCase))
            {
                targetFolder = _modListFileManager.RenameManagedList(ModsFolderPath!, SelectedModList.FolderPath, newName);
            }

            _modListWriter.Write(targetFolder, selectedNames, availableNames);
            _metadataService.Save(
                targetFolder,
                DraftDescription,
                selectedVersions,
                SelectedModList.ModList.CreatedUtc,
                SelectedModList.ModList.LastActivatedUtc);

            FinishEditing();
            StatusMessage = $"Saved {newName}.";
            await RefreshAsync();
            SelectedModList = _allModListItems.FirstOrDefault(item =>
                string.Equals(item.Name, newName, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Save failed", ex.Message);
        }
    }

    private async Task SaveNewListAsync(
        IReadOnlyList<string> selectedNames,
        IReadOnlyList<string> availableNames,
        IReadOnlyDictionary<string, string> selectedVersions)
    {
        var rootSettingsPath = Path.Combine(ModsFolderPath!, FactorioFileNames.ModSettingsDat);
        if (!File.Exists(rootSettingsPath))
        {
            await _dialogService.ShowErrorAsync(
                "Missing mod-settings.dat",
                "The root mods folder has no mod-settings.dat. Create or launch Factorio once so the file exists, then create the mod list.");
            return;
        }

        var createdName = DraftName.Trim();
        var validation = _nameValidator.Validate(createdName, ModsFolderPath!, _allModListItems.Select(list => list.Name));
        if (!validation.IsValid)
        {
            await _dialogService.ShowErrorAsync("Invalid name", validation.Message);
            return;
        }

        var createdFolder = default(string);
        try
        {
            createdFolder = _modListFileManager.CreateManagedListFolder(ModsFolderPath!, createdName);
            _modSettingsManager.CopyRootSettingsToModList(ModsFolderPath!, createdFolder);
            _modListWriter.Write(createdFolder, selectedNames, availableNames);
            _metadataService.Save(createdFolder, DraftDescription, selectedVersions, null, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (!string.IsNullOrWhiteSpace(createdFolder) && Directory.Exists(createdFolder))
            {
                Directory.Delete(createdFolder, recursive: true);
            }

            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Create failed", ex.Message);
            return;
        }

        FinishEditing();
        StatusMessage = $"Created {createdName}.";
        await RefreshAsync();
        SelectedModList = _allModListItems.FirstOrDefault(item =>
            string.Equals(item.Name, createdName, StringComparison.OrdinalIgnoreCase));
    }

    private void CancelEdit()
    {
        FinishEditing();
        LoadSelectedMods();
        StatusMessage = "Edit cancelled.";
    }

    private void FinishEditing()
    {
        _isCreating = false;
        IsEditMode = false;
        foreach (var mod in _allEditableMods)
        {
            mod.PropertyChanged -= EditableModChanged;
        }

        _allEditableMods.Clear();
        EditableMods.Clear();
        DraftName = string.Empty;
        DraftDescription = string.Empty;
        DraftSelectedCount = 0;
        DraftSizeLabel = "-";
    }

    private async Task ActivateSelectedAsync()
    {
        if (SelectedModList is not null)
        {
            await ActivateAsync(SelectedModList);
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (SelectedModList is not null)
        {
            await DeleteAsync(SelectedModList);
        }
    }

    private async Task ActivateAsync(ModListItemViewModel item)
    {
        if (!EnsureWorkspace())
        {
            return;
        }

        var confirmed = await _dialogService.ConfirmAsync(
            "Activate mod list",
            $"Activate {item.Name}?",
            "Confirm");
        if (!confirmed)
        {
            return;
        }

        var result = _modListActivator.Activate(ModsFolderPath!, item.FolderPath);
        if (result.Success)
        {
            try
            {
                _metadataService.RecordActivation(item.FolderPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ErrorMessage = $"Activated, but metadata could not be updated: {ex.Message}";
            }

            try
            {
                await RememberActiveListAsync(item.FolderPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ErrorMessage = $"Activated, but the active mod list could not be remembered: {ex.Message}";
            }

            await _dialogService.ShowMessageAsync(
                "Activation complete",
                $"Activated {item.Name}.\nBackup created at:\n{result.BackupFolderPath}");
            await RefreshAsync();
            return;
        }

        ErrorMessage = result.ErrorMessage;
        await _dialogService.ShowErrorAsync("Activation failed", result.ErrorMessage ?? "Activation failed.");
    }

    private async Task RenameAsync(ModListItemViewModel item)
    {
        if (!EnsureWorkspace())
        {
            return;
        }

        var newName = await _dialogService.PromptAsync("Rename mod list", $"Rename {item.Name}.", item.Name);
        if (string.IsNullOrWhiteSpace(newName) ||
            string.Equals(newName, item.Name, StringComparison.Ordinal))
        {
            return;
        }

        var validation = _nameValidator.Validate(newName, ModsFolderPath!, _allModListItems.Select(list => list.Name), item.Name);
        if (!validation.IsValid)
        {
            await _dialogService.ShowErrorAsync("Invalid name", validation.Message);
            return;
        }

        try
        {
            _modListFileManager.RenameManagedList(ModsFolderPath!, item.FolderPath, newName.Trim());
            StatusMessage = $"Renamed {item.Name} to {newName.Trim()}.";
            await RefreshAsync();
            SelectedModList = _allModListItems.FirstOrDefault(list =>
                string.Equals(list.Name, newName.Trim(), StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Rename failed", ex.Message);
        }
    }

    private async Task DeleteAsync(ModListItemViewModel item)
    {
        if (!EnsureWorkspace())
        {
            return;
        }

        var confirmed = await _dialogService.ConfirmAsync(
            "Delete mod list",
            $"Delete {item.Name}? This only removes the recognized managed mod-list folder.",
            "Delete");
        if (!confirmed)
        {
            return;
        }

        try
        {
            _modListFileManager.DeleteManagedList(ModsFolderPath!, item.FolderPath);
            StatusMessage = $"Deleted {item.Name}.";
            await RefreshAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Delete failed", ex.Message);
        }
    }

    private void LoadSelectedMods()
    {
        SelectedMods.Clear();

        if (SelectedModList is null)
        {
            return;
        }

        var availableByName = BuildAvailableModLookup();
        foreach (var selectedName in SelectedModList.ModList.SelectedMods.OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase))
        {
            if (availableByName.TryGetValue(selectedName, out var modInfo))
            {
                SelectedModList.ModList.SelectedVersions.TryGetValue(selectedName, out var selectedVersion);
                SelectedMods.Add(new DisplayModViewModel
                {
                    Name = modInfo.Name,
                    Title = modInfo.DisplayTitle,
                    Version = string.IsNullOrWhiteSpace(selectedVersion) ? modInfo.DisplayVersion : selectedVersion,
                    Author = modInfo.DisplayAuthor,
                    Description = modInfo.Description,
                    Size = modInfo.DisplaySize
                });
            }
            else
            {
                SelectedMods.Add(new DisplayModViewModel
                {
                    Name = selectedName,
                    Title = selectedName,
                    Version = SelectedModList.ModList.SelectedVersions.TryGetValue(selectedName, out var selectedVersion) ? selectedVersion : null,
                    IsMissing = true
                });
            }
        }
    }

    private Dictionary<string, ModInfo> BuildAvailableModLookup()
    {
        return _availableMods
            .GroupBy(mod => mod.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(mod => mod.HasMetadataWarning)
                    .ThenByDescending(mod => Version.TryParse(mod.Version, out var parsed) ? parsed : new Version(0, 0))
                    .First(),
                StringComparer.OrdinalIgnoreCase);
    }

    private void ApplyEditorFilter()
    {
        EditableMods.Clear();
        var filter = SearchText?.Trim();
        var visible = string.IsNullOrWhiteSpace(filter)
            ? _allEditableMods
            : _allEditableMods.Where(mod =>
                mod.Title.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ||
                mod.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));

        var ordered = IsEditorSortedByActive
            ? visible
                .OrderByDescending(mod => mod.IsSelected)
                .ThenBy(mod => mod.Title, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(mod => mod.Name, StringComparer.OrdinalIgnoreCase)
            : visible
                .OrderBy(mod => mod.Title, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(mod => mod.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var mod in ordered)
        {
            EditableMods.Add(mod);
        }
    }

    private void ApplyListFilter()
    {
        ModLists.Clear();
        var filter = ListSearchText?.Trim();
        var visible = string.IsNullOrWhiteSpace(filter)
            ? _allModListItems
            : _allModListItems.Where(list =>
                list.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ||
                list.Description.Contains(filter, StringComparison.CurrentCultureIgnoreCase));

        foreach (var list in visible.OrderBy(list => list.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            ModLists.Add(list);
        }

        OnPropertyChanged(nameof(VisibleModListCount));
    }

    private void LoadInstalledMods()
    {
        InstalledMods.Clear();
        foreach (var mod in _availableMods.OrderBy(mod => mod.DisplayTitle, StringComparer.CurrentCultureIgnoreCase))
        {
            InstalledMods.Add(new InstalledModViewModel(mod));
        }
    }

    private void EditableModChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditableModViewModel.IsSelected) or nameof(EditableModViewModel.SelectedVersion))
        {
            UpdateDraftSummary();
            if (IsEditorSortedByActive && e.PropertyName == nameof(EditableModViewModel.IsSelected))
            {
                ApplyEditorFilter();
            }
        }
    }

    private void UpdateDraftSummary()
    {
        var selected = _allEditableMods.Where(mod => mod.IsSelected).Select(mod => mod.Name).ToList();
        DraftSelectedCount = selected.Count;
        DraftSizeLabel = CalculateSelectedSizeLabel(selected);
    }

    private async Task<string?> GetValidatedRememberedActiveFolderPathAsync(IReadOnlyList<ModList> detectedModLists)
    {
        var rememberedFolderPath = _settings.ActiveModListFolderPath;
        if (string.IsNullOrWhiteSpace(rememberedFolderPath) || string.IsNullOrWhiteSpace(ModsFolderPath))
        {
            return null;
        }

        var rememberedList = detectedModLists.FirstOrDefault(list => PathsEqual(list.FolderPath, rememberedFolderPath));
        if (rememberedList is not null && _activeModListDetector.IsActive(ModsFolderPath, rememberedList.FolderPath))
        {
            return rememberedList.FolderPath;
        }

        await ClearRememberedActiveListAsync();
        return null;
    }

    private async Task RememberActiveListAsync(string modListFolderPath)
    {
        _settings.LastModsFolderPath = ModsFolderPath;
        _settings.ActiveModListFolderPath = modListFolderPath;
        await _appSettingsService.SaveAsync(_settings);
    }

    private async Task ClearRememberedActiveListAsync()
    {
        if (string.IsNullOrWhiteSpace(_settings.ActiveModListFolderPath))
        {
            return;
        }

        _settings.ActiveModListFolderPath = null;
        try
        {
            await _appSettingsService.SaveAsync(_settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ErrorMessage = $"The remembered active mod list is invalid, but settings could not be updated: {ex.Message}";
        }
    }

    private string CalculateSelectedSizeLabel(IEnumerable<string> selectedModNames)
    {
        var availableByName = BuildAvailableModLookup();
        var bytes = selectedModNames
            .Where(name => availableByName.ContainsKey(name))
            .Sum(name => availableByName[name].TotalSizeBytes > 0 ? availableByName[name].TotalSizeBytes : availableByName[name].SizeBytes);
        return ModInfo.FormatBytes(bytes);
    }

    private string GenerateUniqueDraftName()
    {
        const string baseName = "New Mod List";
        var existing = _allModListItems.Select(list => list.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!existing.Contains(baseName))
        {
            return baseName;
        }

        var suffix = 2;
        while (existing.Contains($"{baseName} {suffix}"))
        {
            suffix++;
        }

        return $"{baseName} {suffix}";
    }

    private void RaiseSelectionCommandStates()
    {
        OnPropertyChanged(nameof(CanActivateSelected));
        EditSelectedCommand.RaiseCanExecuteChanged();
        ActivateSelectedCommand.RaiseCanExecuteChanged();
        DeleteSelectedCommand.RaiseCanExecuteChanged();
    }

    private bool EnsureWorkspace()
    {
        ValidateFolder();
        return IsFolderValid && !string.IsNullOrWhiteSpace(ModsFolderPath);
    }

    private static class EditorSortModes
    {
        public const string Name = "Name";
        public const string Active = "Active";
    }

    private static bool PathsEqual(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        try
        {
            var normalizedLeft = Path.TrimEndingDirectorySeparator(Path.GetFullPath(left));
            var normalizedRight = Path.TrimEndingDirectorySeparator(Path.GetFullPath(right));
            return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}
