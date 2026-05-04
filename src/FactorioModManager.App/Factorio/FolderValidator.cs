using FactorioModManager.App.Models;

namespace FactorioModManager.App.Factorio;

public sealed class FolderValidator
{
    public FolderValidationResult Validate(string? folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return new FolderValidationResult
            {
                IsValid = false,
                Message = "Select your Factorio mods folder to begin."
            };
        }

        if (!Directory.Exists(folderPath))
        {
            return new FolderValidationResult
            {
                IsValid = false,
                Message = "The selected folder does not exist."
            };
        }

        try
        {
            var hasZip = Directory.EnumerateFiles(folderPath, "*.zip", SearchOption.TopDirectoryOnly).Any();
            var hasModList = File.Exists(Path.Combine(folderPath, FactorioFileNames.ModListJson));
            var hasModSettings = File.Exists(Path.Combine(folderPath, FactorioFileNames.ModSettingsDat));

            if (hasZip || hasModList || hasModSettings)
            {
                return new FolderValidationResult
                {
                    IsValid = true,
                    Message = "Folder looks like a Factorio mods folder."
                };
            }

            return new FolderValidationResult
            {
                IsValid = false,
                Message = "This folder has no mod zips, mod-list.json, or mod-settings.dat."
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new FolderValidationResult
            {
                IsValid = false,
                Message = $"Cannot read this folder: {ex.Message}"
            };
        }
    }
}
