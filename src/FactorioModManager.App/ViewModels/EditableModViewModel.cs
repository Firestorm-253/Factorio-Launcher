using FactorioModManager.App.Models;

namespace FactorioModManager.App.ViewModels;

public sealed class EditableModViewModel : ViewModelBase
{
    private bool _isSelected;
    private string _selectedVersion;

    public EditableModViewModel(ModInfo modInfo, bool isSelected, string? selectedVersion)
    {
        Name = modInfo.Name;
        Title = modInfo.DisplayTitle;
        Version = modInfo.Version;
        Author = modInfo.DisplayAuthor;
        Description = modInfo.Description;
        Size = modInfo.DisplaySize;
        AvailableVersions = modInfo.AvailableVersions.Count > 0 ? modInfo.AvailableVersions : [modInfo.DisplayVersion];
        SourceZipPath = modInfo.SourceZipPath;
        HasWarning = modInfo.HasMetadataWarning;
        WarningMessage = modInfo.WarningMessage;
        _isSelected = isSelected;
        _selectedVersion = string.IsNullOrWhiteSpace(selectedVersion)
            ? AvailableVersions.FirstOrDefault() ?? "-"
            : selectedVersion;
    }

    public string Name { get; }
    public string Title { get; }
    public string? Version { get; }
    public string Author { get; }
    public string? Description { get; }
    public string Size { get; }
    public IReadOnlyList<string> AvailableVersions { get; }
    public string NewestVersion => AvailableVersions.FirstOrDefault() ?? "-";
    public string SourceZipPath { get; }
    public bool HasWarning { get; }
    public string? WarningMessage { get; }
    public string Subtitle => string.IsNullOrWhiteSpace(Description) ? $"{Author} - {Name}" : $"{Author} - {Description}";
    public bool HasMultipleVersions => AvailableVersions.Count > 1;
    public bool HasSingleVersion => !HasMultipleVersions;
    public bool IsUsingOlderVersion => HasMultipleVersions &&
        !string.Equals(SelectedVersion, NewestVersion, StringComparison.OrdinalIgnoreCase);
    public string OlderVersionToolTip => $"Selected version is older than latest available version {NewestVersion}.";

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(RowBackground));
            }
        }
    }

    public string RowBackground => IsSelected ? "#241D16" : "#1A1814";

    public string SelectedVersion
    {
        get => _selectedVersion;
        set
        {
            if (SetProperty(ref _selectedVersion, value))
            {
                OnPropertyChanged(nameof(IsUsingOlderVersion));
                OnPropertyChanged(nameof(OlderVersionToolTip));
            }
        }
    }
}
