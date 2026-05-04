using System.Text.Json;
using FactorioModManager.App.Factorio;

namespace FactorioModManager.Tests;

public sealed class ModListReaderWriterTests
{
    [Fact]
    public void Writer_outputs_factorio_shape_with_base_and_enabled_flags()
    {
        using var temp = new TempDirectory();

        new ModListWriter().Write(temp.Path, ["alien-biomes"], ["alien-biomes", "krastorio2"]);

        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(temp.Path, FactorioFileNames.ModListJson)));
        var mods = document.RootElement.GetProperty("mods").EnumerateArray().ToList();

        Assert.Equal("base", mods[0].GetProperty("name").GetString());
        Assert.True(mods[0].GetProperty("enabled").GetBoolean());
        Assert.Contains(mods, mod => mod.GetProperty("name").GetString() == "alien-biomes" && mod.GetProperty("enabled").GetBoolean());
        Assert.Contains(mods, mod => mod.GetProperty("name").GetString() == "krastorio2" && !mod.GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public void Reader_returns_enabled_mods_and_ignores_base()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, FactorioFileNames.ModListJson), """
        {
          "mods": [
            { "name": "base", "enabled": true },
            { "name": "enabled-mod", "enabled": true },
            { "name": "disabled-mod", "enabled": false }
          ]
        }
        """);

        var result = new ModListReader().ReadSelectedMods(temp.Path);

        Assert.Equal(["enabled-mod"], result);
    }
}
