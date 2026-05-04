using System.Text.Json;

namespace FactorioModManager.App.Factorio;

public sealed class ModListReader
{
    public IReadOnlyList<string> ReadSelectedMods(string modListFolderPath)
    {
        var path = Path.Combine(modListFolderPath, FactorioFileNames.ModListJson);
        if (!File.Exists(path))
        {
            return [];
        }

        try
        {
            using var stream = File.OpenRead(path);
            using var document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty("mods", out var modsElement) ||
                modsElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var selected = new List<string>();
            foreach (var modElement in modsElement.EnumerateArray())
            {
                if (!modElement.TryGetProperty("name", out var nameElement) ||
                    nameElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var name = nameElement.GetString();
                if (string.IsNullOrWhiteSpace(name) ||
                    string.Equals(name, "base", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var enabled = true;
                if (modElement.TryGetProperty("enabled", out var enabledElement) &&
                    (enabledElement.ValueKind == JsonValueKind.True || enabledElement.ValueKind == JsonValueKind.False))
                {
                    enabled = enabledElement.GetBoolean();
                }

                if (enabled)
                {
                    selected.Add(name);
                }
            }

            return selected
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }
}
