namespace FactorioModManager.App.Factorio;

public sealed class BackupService
{
    public string CreateBackup(string modsFolderPath)
    {
        var rootModList = Path.Combine(modsFolderPath, FactorioFileNames.ModListJson);
        var rootModSettings = Path.Combine(modsFolderPath, FactorioFileNames.ModSettingsDat);

        if (!File.Exists(rootModList))
        {
            throw new FileNotFoundException("Root mod-list.json is missing.", rootModList);
        }

        if (!File.Exists(rootModSettings))
        {
            throw new FileNotFoundException("Root mod-settings.dat is missing.", rootModSettings);
        }

        var backupRoot = ManagerWorkspacePaths.GetBackupsRoot(modsFolderPath);
        Directory.CreateDirectory(backupRoot);

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var backupFolder = Path.Combine(backupRoot, timestamp);
        var suffix = 1;
        while (Directory.Exists(backupFolder))
        {
            backupFolder = Path.Combine(backupRoot, $"{timestamp}_{suffix}");
            suffix++;
        }

        Directory.CreateDirectory(backupFolder);
        File.Copy(rootModList, Path.Combine(backupFolder, FactorioFileNames.ModListJson));
        File.Copy(rootModSettings, Path.Combine(backupFolder, FactorioFileNames.ModSettingsDat));
        return backupFolder;
    }
}
