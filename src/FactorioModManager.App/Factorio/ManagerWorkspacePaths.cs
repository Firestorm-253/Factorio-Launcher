namespace FactorioModManager.App.Factorio;

public static class ManagerWorkspacePaths
{
    public static string GetRoot(string modsFolderPath)
    {
        return Path.Combine(modsFolderPath, FactorioFileNames.ManagerRootFolder);
    }

    public static string GetListsRoot(string modsFolderPath)
    {
        return Path.Combine(GetRoot(modsFolderPath), FactorioFileNames.ManagerListsFolder);
    }

    public static string GetBackupsRoot(string modsFolderPath)
    {
        return Path.Combine(GetRoot(modsFolderPath), FactorioFileNames.ManagerBackupsFolder);
    }

    public static string GetManagedListFolder(string modsFolderPath, string name)
    {
        return Path.Combine(GetListsRoot(modsFolderPath), name);
    }

    public static bool IsManagedListPath(string modsFolderPath, string modListFolderPath)
    {
        return IsImmediateChild(GetListsRoot(modsFolderPath), modListFolderPath);
    }

    public static bool IsImmediateChild(string parentFolder, string childFolder)
    {
        var parent = Path.TrimEndingDirectorySeparator(Path.GetFullPath(parentFolder));
        var child = Path.TrimEndingDirectorySeparator(Path.GetFullPath(childFolder));
        return string.Equals(Path.GetDirectoryName(child), parent, StringComparison.OrdinalIgnoreCase);
    }
}
