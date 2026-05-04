using FactorioModManager.App.Models;

namespace FactorioModManager.App.Factorio;

public sealed class ModListDetector
{
    private readonly ModListReader _reader;
    private readonly ModListMetadataService _metadataService;

    public ModListDetector(ModListReader reader, ModListMetadataService? metadataService = null)
    {
        _reader = reader;
        _metadataService = metadataService ?? new ModListMetadataService();
    }

    public IReadOnlyList<ModList> Detect(string modsFolderPath)
    {
        var listsRoot = ManagerWorkspacePaths.GetListsRoot(modsFolderPath);
        if (!Directory.Exists(listsRoot))
        {
            return [];
        }

        try
        {
            return Directory.EnumerateDirectories(listsRoot, "*", SearchOption.TopDirectoryOnly)
                .Where(IsManagedListFolder)
                .Select(CreateModList)
                .OrderBy(list => list.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private ModList CreateModList(string folder)
    {
        var metadata = _metadataService.Load(folder);
        return new ModList
        {
            Name = Path.GetFileName(folder),
            FolderPath = folder,
            SelectedMods = _reader.ReadSelectedMods(folder),
            Description = metadata.Description,
            SelectedVersions = new Dictionary<string, string>(metadata.SelectedVersions, StringComparer.OrdinalIgnoreCase),
            CreatedUtc = metadata.CreatedUtc,
            UpdatedUtc = metadata.UpdatedUtc,
            LastActivatedUtc = metadata.LastActivatedUtc
        };
    }

    public static bool IsManagedListFolder(string folderPath)
    {
        return Directory.Exists(folderPath) &&
            File.Exists(Path.Combine(folderPath, FactorioFileNames.ModListJson)) &&
            File.Exists(Path.Combine(folderPath, FactorioFileNames.ModSettingsDat));
    }
}
