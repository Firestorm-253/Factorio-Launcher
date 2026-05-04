namespace FactorioModManager.App.Factorio;

public sealed class ModSettingsManager
{
    public void CopyRootSettingsToModList(string modsFolderPath, string modListFolderPath)
    {
        var source = Path.Combine(modsFolderPath, FactorioFileNames.ModSettingsDat);
        var destination = Path.Combine(modListFolderPath, FactorioFileNames.ModSettingsDat);

        if (!File.Exists(source))
        {
            throw new FileNotFoundException("Root mod-settings.dat is missing.", source);
        }

        Directory.CreateDirectory(modListFolderPath);
        File.Copy(source, destination, overwrite: true);
    }
}
