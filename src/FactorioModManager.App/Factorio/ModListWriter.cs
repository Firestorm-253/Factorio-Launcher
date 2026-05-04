using System.Text.Json;

namespace FactorioModManager.App.Factorio;

public sealed class ModListWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public void Write(string modListFolderPath, IEnumerable<string> selectedModNames, IEnumerable<string> availableModNames)
    {
        Directory.CreateDirectory(modListFolderPath);
        var selected = selectedModNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var available = availableModNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Where(name => !string.Equals(name, "base", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var payload = new FactorioModListJson
        {
            Mods =
            [
                new FactorioModJson
                {
                    Name = "base",
                    Enabled = true
                },
                .. available.Select(name => new FactorioModJson
                {
                    Name = name,
                    Enabled = selected.Contains(name)
                })
            ]
        };

        var path = Path.Combine(modListFolderPath, FactorioFileNames.ModListJson);
        using var stream = File.Create(path);
        JsonSerializer.Serialize(stream, payload, JsonOptions);
    }

    private sealed class FactorioModListJson
    {
        public required IReadOnlyList<FactorioModJson> Mods { get; init; }
    }

    private sealed class FactorioModJson
    {
        public required string Name { get; init; }
        public required bool Enabled { get; init; }
    }
}
