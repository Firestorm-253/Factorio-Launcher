using FactorioModManager.App.Models;

namespace FactorioModManager.App.Factorio;

public sealed class NameValidator
{
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    public NameValidationResult Validate(string? name, string modsFolderPath, IEnumerable<string> existingNames, string? currentName = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return NameValidationResult.Invalid("Enter a mod-list name.");
        }

        var trimmed = name.Trim();
        if (!string.Equals(trimmed, name, StringComparison.Ordinal))
        {
            return NameValidationResult.Invalid("Names cannot begin or end with spaces.");
        }

        if (trimmed.StartsWith(".", StringComparison.Ordinal))
        {
            return NameValidationResult.Invalid("Names cannot begin with a period.");
        }

        if (trimmed.IndexOfAny(InvalidFileNameChars) >= 0 ||
            trimmed.Contains(Path.DirectorySeparatorChar) ||
            trimmed.Contains(Path.AltDirectorySeparatorChar))
        {
            return NameValidationResult.Invalid("Names cannot contain filesystem path characters.");
        }

        if (trimmed == "." || trimmed == ".." || trimmed.Contains("..", StringComparison.Ordinal))
        {
            return NameValidationResult.Invalid("Names cannot use path traversal.");
        }

        var listsRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(ManagerWorkspacePaths.GetListsRoot(modsFolderPath)));
        var destination = Path.GetFullPath(ManagerWorkspacePaths.GetManagedListFolder(modsFolderPath, trimmed));
        if (!string.Equals(Path.GetDirectoryName(destination), listsRoot, StringComparison.OrdinalIgnoreCase))
        {
            return NameValidationResult.Invalid("Name must resolve inside the manager lists folder.");
        }

        var duplicate = existingNames.Any(existing =>
            !string.Equals(existing, currentName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(existing, trimmed, StringComparison.OrdinalIgnoreCase));

        if (duplicate || Directory.Exists(destination) && !string.Equals(trimmed, currentName, StringComparison.OrdinalIgnoreCase))
        {
            return NameValidationResult.Invalid("A mod list with this name already exists.");
        }

        return NameValidationResult.Valid();
    }
}
