namespace FactorioModManager.App.Factorio;

public sealed class ModSettingsManager
{
    public void CopyRootSettingsToModList(string modsFolderPath, string modListFolderPath)
    {
        var source = Path.Combine(modsFolderPath, FactorioFileNames.ModSettingsDat);
        CopySettingsToModList(source, modListFolderPath, "Root mod-settings.dat is missing.");
    }

    public void CopySettingsToModList(string sourceSettingsPath, string modListFolderPath, string missingSourceMessage = "mod-settings.dat is missing.")
    {
        if (!File.Exists(sourceSettingsPath))
        {
            throw new FileNotFoundException(missingSourceMessage, sourceSettingsPath);
        }

        var destination = Path.Combine(modListFolderPath, FactorioFileNames.ModSettingsDat);
        Directory.CreateDirectory(modListFolderPath);
        File.Copy(sourceSettingsPath, destination, overwrite: true);
    }
}
