using System.IO.Compression;
using FactorioModManager.App.Factorio;

namespace FactorioModManager.Tests;

public sealed class ModScannerTests
{
    [Fact]
    public void Scan_uses_only_root_mod_sources()
    {
        using var temp = new TempDirectory();
        CreateZip(Path.Combine(temp.Path, "root-mod_1.0.0.zip"), "root-mod", "Root Mod", "1.0.0", "Author", "Root description");
        CreateUnpackedMod(temp.Path, "folder-mod_2.0.2", "folder-mod", "Folder Mod", "2.0.2", "Author", "Folder description");
        Directory.CreateDirectory(Path.Combine(temp.Path, "Nested"));
        CreateZip(Path.Combine(temp.Path, "Nested", "nested-mod_1.0.0.zip"), "nested-mod", "Nested Mod", "1.0.0", "Author", "Nested description");
        CreateUnpackedMod(ManagerWorkspacePaths.GetListsRoot(temp.Path), "managed-list-looking-mod_1.0.0", "managed-list-looking-mod", "Managed", "1.0.0");
        File.WriteAllText(Path.Combine(temp.Path, "readme.txt"), "ignored");

        var results = new ModScanner(new ModInfoReader()).Scan(temp.Path);

        Assert.Equal(["folder-mod", "root-mod"], results.Select(mod => mod.Name).OrderBy(name => name, StringComparer.Ordinal));
    }

    [Fact]
    public void Scan_deduplicates_by_internal_mod_name_and_keeps_latest_metadata()
    {
        using var temp = new TempDirectory();
        var oldZip = Path.Combine(temp.Path, "duplicate-mod_1.0.0.zip");
        var newZip = Path.Combine(temp.Path, "duplicate-mod_1.2.0.zip");
        CreateZip(oldZip, "duplicate-mod", "Duplicate Mod", "1.0.0", "Earendel", "Old version");
        CreateZip(newZip, "duplicate-mod", "Duplicate Mod", "1.2.0", "Earendel", "New version");

        var results = new ModScanner(new ModInfoReader()).Scan(temp.Path);

        var result = Assert.Single(results);
        Assert.Equal("duplicate-mod", result.Name);
        Assert.Equal("1.2.0", result.Version);
        Assert.Equal(["1.2.0", "1.0.0"], result.AvailableVersions);
        Assert.Equal("Earendel", result.Author);
        Assert.Equal("New version", result.Description);
        Assert.Equal(new FileInfo(oldZip).Length + new FileInfo(newZip).Length, result.TotalSizeBytes);
    }

    [Fact]
    public void Scan_deduplicates_zip_and_unpacked_folder_by_internal_mod_name()
    {
        using var temp = new TempDirectory();
        var zipPath = Path.Combine(temp.Path, "duplicate-mod_1.0.0.zip");
        CreateZip(zipPath, "duplicate-mod", "Duplicate Mod", "1.0.0", "ZipAuthor", "Zip version");
        var folderPath = CreateUnpackedMod(temp.Path, "duplicate-mod_1.2.0", "duplicate-mod", "Duplicate Mod", "1.2.0", "FolderAuthor", "Folder version");

        var results = new ModScanner(new ModInfoReader()).Scan(temp.Path);

        var result = Assert.Single(results);
        Assert.Equal("duplicate-mod", result.Name);
        Assert.Equal("1.2.0", result.Version);
        Assert.Equal(["1.2.0", "1.0.0"], result.AvailableVersions);
        Assert.Equal("FolderAuthor", result.Author);
        Assert.Contains(zipPath, result.SourceZipPaths);
        Assert.Contains(folderPath, result.SourceZipPaths);
    }

    internal static void CreateZip(
        string zipPath,
        string name,
        string title,
        string version,
        string author = "Author",
        string description = "Description")
    {
        var rootFolderName = $"{name}_{version}";
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        var entry = archive.CreateEntry($"{rootFolderName}/info.json");
        using var writer = new StreamWriter(entry.Open());
        writer.Write($$"""
        {"name":"{{name}}","title":"{{title}}","version":"{{version}}","author":"{{author}}","description":"{{description}}"}
        """);
    }

    internal static string CreateUnpackedMod(
        string rootPath,
        string folderName,
        string name,
        string title,
        string version,
        string author = "Author",
        string description = "Description")
    {
        var folderPath = Path.Combine(rootPath, folderName);
        Directory.CreateDirectory(folderPath);
        File.WriteAllText(Path.Combine(folderPath, "info.json"), $$"""
        {"name":"{{name}}","title":"{{title}}","version":"{{version}}","author":"{{author}}","description":"{{description}}"}
        """);
        return folderPath;
    }
}
