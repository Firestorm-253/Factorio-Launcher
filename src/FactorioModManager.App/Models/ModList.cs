namespace FactorioModManager.App.Models;

public sealed class ModList
{
    public required string Name { get; init; }
    public required string FolderPath { get; init; }
    public required IReadOnlyList<string> SelectedMods { get; init; }
    public string Description { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> SelectedVersions { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public DateTimeOffset? CreatedUtc { get; init; }
    public DateTimeOffset? UpdatedUtc { get; init; }
    public DateTimeOffset? LastActivatedUtc { get; init; }
}
