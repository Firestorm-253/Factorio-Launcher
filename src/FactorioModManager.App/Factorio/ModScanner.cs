using FactorioModManager.App.Models;

namespace FactorioModManager.App.Factorio;

public sealed class ModScanner
{
    private static readonly HashSet<string> SupportedFactorioDataMods = new(StringComparer.OrdinalIgnoreCase)
    {
        "elevated-rails",
        "quality",
        "space-age"
    };

    private readonly ModInfoReader _reader;

    public ModScanner(ModInfoReader reader)
    {
        _reader = reader;
    }

    public IReadOnlyList<ModInfo> Scan(string modsFolderPath, string? factorioInstallFolderPath = null)
    {
        if (!Directory.Exists(modsFolderPath))
        {
            return [];
        }

        var zippedMods = Directory.EnumerateFiles(modsFolderPath, "*.zip", SearchOption.TopDirectoryOnly)
            .Select(_reader.Read);
        var unpackedMods = Directory.EnumerateDirectories(modsFolderPath, "*", SearchOption.TopDirectoryOnly)
            .Where(folder => !IsManagerFolder(modsFolderPath, folder))
            .Where(folder => File.Exists(Path.Combine(folder, "info.json")))
            .Select(_reader.ReadDirectory);
        var factorioDataMods = ScanFactorioDataMods(factorioInstallFolderPath);

        return zippedMods
            .Concat(unpackedMods)
            .Concat(factorioDataMods)
            .GroupBy(mod => mod.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ChoosePreferredDuplicate)
            .OrderBy(mod => mod.DisplayTitle, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(mod => mod.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private IEnumerable<ModInfo> ScanFactorioDataMods(string? factorioInstallFolderPath)
    {
        if (string.IsNullOrWhiteSpace(factorioInstallFolderPath))
        {
            return [];
        }

        var dataFolderPath = Path.Combine(factorioInstallFolderPath, "data");
        if (!Directory.Exists(dataFolderPath))
        {
            return [];
        }

        return Directory
            .EnumerateDirectories(dataFolderPath, "*", SearchOption.TopDirectoryOnly)
            .Where(folder => SupportedFactorioDataMods.Contains(Path.GetFileName(folder)))
            .Where(folder => File.Exists(Path.Combine(folder, "info.json")))
            .Select(_reader.ReadDirectory);
    }

    private static bool IsManagerFolder(string modsFolderPath, string folderPath)
    {
        var managerRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(ManagerWorkspacePaths.GetRoot(modsFolderPath)));
        var candidate = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folderPath));
        return string.Equals(managerRoot, candidate, StringComparison.OrdinalIgnoreCase);
    }

    private static ModInfo ChoosePreferredDuplicate(IGrouping<string, ModInfo> duplicateGroup)
    {
        var mods = duplicateGroup.ToList();
        var orderedMods = mods
            .OrderBy(mod => mod.HasMetadataWarning)
            .ThenByDescending(mod => ParseVersionOrDefault(mod.Version))
            .ThenBy(mod => mod.SourceZipPath, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var preferred = orderedMods.First();

        var versions = mods
            .Select(mod => mod.Version)
            .Where(version => !string.IsNullOrWhiteSpace(version))
            .Select(version => version!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(ParseVersionOrDefault)
            .ThenBy(version => version, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ModInfo
        {
            Name = preferred.Name,
            Title = FirstNonEmpty(orderedMods.Select(mod => mod.Title)) ?? preferred.Name,
            Version = versions.FirstOrDefault() ?? preferred.Version,
            Author = FirstNonEmpty(orderedMods.Select(mod => mod.Author)),
            Description = FirstNonEmpty(orderedMods.Select(mod => mod.Description)),
            SourceZipPath = preferred.SourceZipPath,
            SourceZipPaths = mods
                .SelectMany(mod => mod.SourceZipPaths.Count > 0 ? mod.SourceZipPaths : new[] { mod.SourceZipPath })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            AvailableVersions = versions,
            SizeBytes = preferred.SizeBytes,
            TotalSizeBytes = mods.Sum(mod => mod.TotalSizeBytes > 0 ? mod.TotalSizeBytes : mod.SizeBytes),
            HasMetadataWarning = mods.Any(mod => mod.HasMetadataWarning),
            WarningMessage = BuildWarning(mods)
        };
    }

    private static Version ParseVersionOrDefault(string? version)
    {
        return Version.TryParse(version, out var parsed)
            ? parsed
            : new Version(0, 0);
    }

    private static string? FirstNonEmpty(IEnumerable<string?> values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static string? BuildWarning(IReadOnlyCollection<ModInfo> mods)
    {
        var warningCount = mods.Count(mod => mod.HasMetadataWarning);
        return warningCount == 0
            ? null
            : $"{warningCount} mod source(s) had metadata warnings.";
    }
}
