using FactorioModManager.App.Factorio;

namespace FactorioModManager.Tests;

public sealed class ModListDetectorTests
{
    [Fact]
    public void Detect_returns_only_subfolders_with_both_required_files()
    {
        using var temp = new TempDirectory();
        var managedFolder = CreateManagedList(temp.Path, "SpaceExploration");
        new ModListMetadataService().Save(
            managedFolder,
            "Earendel overhaul",
            new Dictionary<string, string> { ["space-exploration"] = "0.6.128" },
            null,
            null);
        Directory.CreateDirectory(ManagerWorkspacePaths.GetManagedListFolder(temp.Path, "Incomplete"));
        File.WriteAllText(Path.Combine(ManagerWorkspacePaths.GetManagedListFolder(temp.Path, "Incomplete"), FactorioFileNames.ModListJson), "{}");
        var legacyRootFolder = Path.Combine(temp.Path, "LegacyRootFolder");
        Directory.CreateDirectory(legacyRootFolder);
        File.WriteAllText(Path.Combine(legacyRootFolder, FactorioFileNames.ModListJson), """{"mods":[]}""");
        File.WriteAllBytes(Path.Combine(legacyRootFolder, FactorioFileNames.ModSettingsDat), [4, 5, 6]);

        var results = new ModListDetector(new ModListReader(), new ModListMetadataService()).Detect(temp.Path);

        var result = Assert.Single(results);
        Assert.Equal("SpaceExploration", result.Name);
        Assert.Equal("Earendel overhaul", result.Description);
        Assert.Equal("0.6.128", result.SelectedVersions["space-exploration"]);
    }

    private static string CreateManagedList(string root, string name)
    {
        var folder = ManagerWorkspacePaths.GetManagedListFolder(root, name);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, FactorioFileNames.ModListJson), """{"mods":[]}""");
        File.WriteAllBytes(Path.Combine(folder, FactorioFileNames.ModSettingsDat), [1, 2, 3]);
        return folder;
    }
}
