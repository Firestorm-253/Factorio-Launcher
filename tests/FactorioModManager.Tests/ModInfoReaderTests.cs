using System.IO.Compression;
using FactorioModManager.App.Factorio;

namespace FactorioModManager.Tests;

public sealed class ModInfoReaderTests
{
    [Fact]
    public void Read_extracts_metadata_from_info_json()
    {
        using var temp = new TempDirectory();
        var zipPath = Path.Combine(temp.Path, "space-exploration_1.2.3.zip");
        CreateZip(zipPath, "space-exploration_1.2.3/info.json", """
        {
          "name": "space-exploration",
          "title": "Space Exploration",
          "version": "1.2.3"
        }
        """);

        var result = new ModInfoReader().Read(zipPath);

        Assert.Equal("space-exploration", result.Name);
        Assert.Equal("Space Exploration", result.Title);
        Assert.Equal("1.2.3", result.Version);
        Assert.False(result.HasMetadataWarning);
    }

    [Fact]
    public void Read_falls_back_for_malformed_zip()
    {
        using var temp = new TempDirectory();
        var zipPath = Path.Combine(temp.Path, "broken-mod_2.0.1.zip");
        File.WriteAllText(zipPath, "not a zip");

        var result = new ModInfoReader().Read(zipPath);

        Assert.Equal("broken-mod", result.Name);
        Assert.Equal("2.0.1", result.Version);
        Assert.True(result.HasMetadataWarning);
    }

    [Fact]
    public void ReadDirectory_extracts_metadata_from_unpacked_mod_folder()
    {
        using var temp = new TempDirectory();
        var folderPath = Path.Combine(temp.Path, "FasterStart_Firestorm_2.0.2");
        Directory.CreateDirectory(folderPath);
        File.WriteAllText(Path.Combine(folderPath, "info.json"), """
        {
          "name": "FasterStart_Firestorm",
          "title": "Faster Start Firestorm",
          "version": "2.0.2"
        }
        """);

        var result = new ModInfoReader().ReadDirectory(folderPath);

        Assert.Equal("FasterStart_Firestorm", result.Name);
        Assert.Equal("Faster Start Firestorm", result.Title);
        Assert.Equal("2.0.2", result.Version);
        Assert.False(result.HasMetadataWarning);
    }

    [Fact]
    public void ReadDirectory_falls_back_from_folder_name_when_info_json_is_missing()
    {
        using var temp = new TempDirectory();
        var folderPath = Path.Combine(temp.Path, "folder-mod_3.1.4");
        Directory.CreateDirectory(folderPath);

        var result = new ModInfoReader().ReadDirectory(folderPath);

        Assert.Equal("folder-mod", result.Name);
        Assert.Equal("3.1.4", result.Version);
        Assert.True(result.HasMetadataWarning);
    }

    private static void CreateZip(string zipPath, string entryName, string contents)
    {
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        var entry = archive.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(contents);
    }
}
