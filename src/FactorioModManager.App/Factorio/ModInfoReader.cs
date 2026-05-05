using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using FactorioModManager.App.Models;

namespace FactorioModManager.App.Factorio;

public sealed class ModInfoReader
{
    private static readonly Regex FileNamePattern = new(
        "^(?<name>.+)_(?<version>\\d+\\.\\d+\\.\\d+(?:\\.\\d+)?)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public ModInfo Read(string zipPath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            var infoEntry = archive.Entries
                .Where(entry => string.Equals(Path.GetFileName(entry.FullName), "info.json", StringComparison.OrdinalIgnoreCase))
                .OrderBy(entry => entry.FullName.Count(ch => ch == '/' || ch == '\\'))
                .FirstOrDefault();

            if (infoEntry is null)
            {
                return CreateFallback(zipPath, "No info.json was found in the zip.");
            }

            using var stream = infoEntry.Open();
            using var document = JsonDocument.Parse(stream);
            return CreateFromInfoJson(zipPath, document.RootElement, () => CreateFallback(zipPath, "info.json did not contain a mod name."));
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or UnauthorizedAccessException)
        {
            return CreateFallback(zipPath, $"Could not read mod metadata: {ex.Message}");
        }
    }

    public ModInfo ReadDirectory(string modFolderPath)
    {
        try
        {
            var infoPath = Path.Combine(modFolderPath, "info.json");
            if (!File.Exists(infoPath))
            {
                return CreateFallback(modFolderPath, "No info.json was found in the folder.");
            }

            using var stream = File.OpenRead(infoPath);
            using var document = JsonDocument.Parse(stream);
            return CreateFromInfoJson(modFolderPath, document.RootElement, () => CreateFallback(modFolderPath, "info.json did not contain a mod name."));
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return CreateFallback(modFolderPath, $"Could not read mod metadata: {ex.Message}");
        }
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static ModInfo CreateFromInfoJson(string sourcePath, JsonElement root, Func<ModInfo> createFallback)
    {
        var name = ReadString(root, "name");
        var title = ReadString(root, "title");
        var version = ReadString(root, "version");
        var author = ReadString(root, "author");
        var description = ReadString(root, "description");

        if (string.IsNullOrWhiteSpace(name))
        {
            return createFallback().WithMetadata(title, version, author, description);
        }

        var size = GetSourceSize(sourcePath);
        return new ModInfo
        {
            Name = name,
            Title = string.IsNullOrWhiteSpace(title) ? name : title,
            Version = version,
            Author = author,
            Description = description,
            SourceZipPath = sourcePath,
            SourceZipPaths = [sourcePath],
            AvailableVersions = string.IsNullOrWhiteSpace(version) ? [] : [version],
            SizeBytes = size,
            TotalSizeBytes = size
        };
    }

    private static ModInfo CreateFallback(string sourcePath, string warning)
    {
        var stem = Directory.Exists(sourcePath)
            ? Path.GetFileName(sourcePath)
            : Path.GetFileNameWithoutExtension(sourcePath);
        var match = FileNamePattern.Match(stem);
        var name = match.Success ? match.Groups["name"].Value : stem;
        var version = match.Success ? match.Groups["version"].Value : null;
        var size = GetSourceSize(sourcePath);

        return new ModInfo
        {
            Name = name,
            Title = name,
            Version = version,
            SourceZipPath = sourcePath,
            SourceZipPaths = [sourcePath],
            AvailableVersions = string.IsNullOrWhiteSpace(version) ? [] : [version],
            SizeBytes = size,
            TotalSizeBytes = size,
            HasMetadataWarning = true,
            WarningMessage = warning
        };
    }

    private static long GetSourceSize(string sourcePath)
    {
        try
        {
            if (File.Exists(sourcePath))
            {
                return new FileInfo(sourcePath).Length;
            }

            if (Directory.Exists(sourcePath))
            {
                return Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories)
                    .Sum(path => new FileInfo(path).Length);
            }

            return 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return 0;
        }
    }
}

file static class ModInfoExtensions
{
    public static ModInfo WithMetadata(this ModInfo modInfo, string? title, string? version, string? author, string? description)
    {
        return new ModInfo
        {
            Name = modInfo.Name,
            Title = string.IsNullOrWhiteSpace(title) ? modInfo.Title : title,
            Version = string.IsNullOrWhiteSpace(version) ? modInfo.Version : version,
            Author = string.IsNullOrWhiteSpace(author) ? modInfo.Author : author,
            Description = string.IsNullOrWhiteSpace(description) ? modInfo.Description : description,
            SourceZipPath = modInfo.SourceZipPath,
            SourceZipPaths = modInfo.SourceZipPaths,
            AvailableVersions = string.IsNullOrWhiteSpace(version) ? modInfo.AvailableVersions : [version],
            SizeBytes = modInfo.SizeBytes,
            TotalSizeBytes = modInfo.TotalSizeBytes,
            HasMetadataWarning = modInfo.HasMetadataWarning,
            WarningMessage = modInfo.WarningMessage
        };
    }
}
