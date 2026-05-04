using FactorioModManager.App.Factorio;

namespace FactorioModManager.Tests;

public sealed class ModListFileManagerTests
{
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
        var folder = Path.Combine(temp.Path, "Old");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, FactorioFileNames.ModListJson), """{"mods":[]}""");
        File.WriteAllBytes(Path.Combine(folder, FactorioFileNames.ModSettingsDat), [1]);

        var newPath = new ModListFileManager().RenameManagedList(temp.Path, folder, "New");

        Assert.False(Directory.Exists(folder));
        Assert.True(Directory.Exists(newPath));
        Assert.True(File.Exists(Path.Combine(newPath, FactorioFileNames.ModSettingsDat)));
    }
}
