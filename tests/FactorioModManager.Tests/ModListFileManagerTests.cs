using FactorioModManager.App.Factorio;

namespace FactorioModManager.Tests;

public sealed class ModListFileManagerTests
{
    [Fact]
    public void CreateManagedListFolder_creates_inside_manager_lists_folder()
    {
        using var temp = new TempDirectory();

        var folder = new ModListFileManager().CreateManagedListFolder(temp.Path, "New");

        Assert.True(Directory.Exists(folder));
        Assert.Equal(ManagerWorkspacePaths.GetManagedListFolder(temp.Path, "New"), folder);
        Assert.False(Directory.Exists(Path.Combine(temp.Path, "New")));
    }

    [Fact]
    public void DeleteManagedList_rejects_unmanaged_folder()
    {
        using var temp = new TempDirectory();
        var folder = Path.Combine(temp.Path, "Unknown");
        Directory.CreateDirectory(folder);

        Assert.Throws<InvalidOperationException>(() =>
            new ModListFileManager().DeleteManagedList(temp.Path, folder));
        Assert.True(Directory.Exists(folder));
    }

    [Fact]
    public void RenameManagedList_renames_only_managed_folder()
    {
        using var temp = new TempDirectory();
        var folder = ManagerWorkspacePaths.GetManagedListFolder(temp.Path, "Old");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, FactorioFileNames.ModListJson), """{"mods":[]}""");
        File.WriteAllBytes(Path.Combine(folder, FactorioFileNames.ModSettingsDat), [1]);

        var newPath = new ModListFileManager().RenameManagedList(temp.Path, folder, "New");

        Assert.False(Directory.Exists(folder));
        Assert.True(Directory.Exists(newPath));
        Assert.True(File.Exists(Path.Combine(newPath, FactorioFileNames.ModSettingsDat)));
        Assert.StartsWith(ManagerWorkspacePaths.GetListsRoot(temp.Path), newPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeleteManagedList_rejects_root_level_managed_looking_folder()
    {
        using var temp = new TempDirectory();
        var folder = Path.Combine(temp.Path, "OldRootLayout");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, FactorioFileNames.ModListJson), """{"mods":[]}""");
        File.WriteAllBytes(Path.Combine(folder, FactorioFileNames.ModSettingsDat), [1]);

        Assert.Throws<InvalidOperationException>(() =>
            new ModListFileManager().DeleteManagedList(temp.Path, folder));
        Assert.True(Directory.Exists(folder));
    }
}
