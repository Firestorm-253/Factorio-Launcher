using System.IO.Compression;
using FactorioModManager.App.Factorio;

namespace FactorioModManager.Tests;

public sealed class ModScannerTests
{
    [Fact]
    public void Scan_uses_only_root_zip_files()
    {
        using var temp = new TempDirectory();
        CreateZip(Path.Combine(temp.Path, "root-mod_1.0.0.zip"), "root-mod", "Root Mod", "1.0.0", "Author", "Root description");
        Directory.CreateDirectory(Path.Combine(temp.Path, "Nested"));
        CreateZip(Path.Combine(temp.Path, "Nested", "nested-mod_1.0.0.zip"), "nested-mod", "Nested Mod", "1.0.0", "Author", "Nested description");
        File.WriteAllText(Path.Combine(temp.Path, "readme.txt"), "ignored");

        var results = new ModScanner(new ModInfoReader()).Scan(temp.Path);

        Assert.Single(results);
        Assert.Equal("root-mod", results[0].Name);
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
}
