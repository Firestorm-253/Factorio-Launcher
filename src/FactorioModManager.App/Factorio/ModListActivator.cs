using FactorioModManager.App.Models;

namespace FactorioModManager.App.Factorio;

public sealed class ModListActivator
{
    private readonly BackupService _backupService;

    public ModListActivator(BackupService backupService)
    {
        _backupService = backupService;
    }

    public ModListActivationResult Activate(string modsFolderPath, string modListFolderPath)
    {
        var backupFolder = default(string);
        var rootFilesModified = false;

        try
        {
            if (!Directory.Exists(modsFolderPath))
            {
                return Failure("The Factorio mods folder does not exist.", backupFolder, rootFilesModified);
            }

            if (!ManagerWorkspacePaths.IsManagedListPath(modsFolderPath, modListFolderPath))
            {
                return Failure("The selected mod list is not inside the manager lists folder.", backupFolder, rootFilesModified);
            }

            if (!ModListDetector.IsManagedListFolder(modListFolderPath))
            {
                return Failure("The selected folder is not a managed mod list.", backupFolder, rootFilesModified);
            }

            var sourceModList = Path.Combine(modListFolderPath, FactorioFileNames.ModListJson);
            var sourceModSettings = Path.Combine(modListFolderPath, FactorioFileNames.ModSettingsDat);
            var rootModList = Path.Combine(modsFolderPath, FactorioFileNames.ModListJson);
            var rootModSettings = Path.Combine(modsFolderPath, FactorioFileNames.ModSettingsDat);

            using (File.OpenRead(sourceModList))
            {
            }

            using (File.OpenRead(sourceModSettings))
            {
            }

            backupFolder = _backupService.CreateBackup(modsFolderPath);

            File.Copy(sourceModList, rootModList, overwrite: true);
            rootFilesModified = true;
            File.Copy(sourceModSettings, rootModSettings, overwrite: true);

            return new ModListActivationResult
            {
                Success = true,
                BackupFolderPath = backupFolder,
                RootFilesModified = true
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var message = rootFilesModified
                ? $"Activation failed after one root file was modified. Backup folder: {backupFolder}. Error: {ex.Message}"
                : $"Activation failed before root files were modified. Error: {ex.Message}";
            return Failure(message, backupFolder, rootFilesModified);
        }
    }

    private static ModListActivationResult Failure(string message, string? backupFolderPath, bool rootFilesModified)
    {
        return new ModListActivationResult
        {
            Success = false,
            ErrorMessage = message,
            BackupFolderPath = backupFolderPath,
            RootFilesModified = rootFilesModified
        };
    }

    internal static bool IsImmediateChild(string parentFolder, string childFolder)
    {
        return ManagerWorkspacePaths.IsImmediateChild(parentFolder, childFolder);
    }
}
