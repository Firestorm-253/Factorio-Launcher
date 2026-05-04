namespace FactorioModManager.App.Factorio;

public sealed class ModListFileManager
{
    public string CreateManagedListFolder(string modsFolderPath, string name)
    {
        var folderPath = Path.Combine(modsFolderPath, name);
        if (Directory.Exists(folderPath))
        {
            throw new IOException("A folder with this mod-list name already exists.");
        }

        Directory.CreateDirectory(folderPath);
        return folderPath;
    }

    public string RenameManagedList(string modsFolderPath, string currentFolderPath, string newName)
    {
        if (!ModListActivator.IsImmediateChild(modsFolderPath, currentFolderPath) ||
            !ModListDetector.IsManagedListFolder(currentFolderPath))
        {
            throw new InvalidOperationException("Only recognized managed mod-list folders can be renamed.");
        }

        var destination = Path.Combine(modsFolderPath, newName);
        if (Directory.Exists(destination))
        {
            throw new IOException("A folder with this mod-list name already exists.");
        }

        Directory.Move(currentFolderPath, destination);
        return destination;
    }

    public void DeleteManagedList(string modsFolderPath, string folderPath)
    {
        if (!ModListActivator.IsImmediateChild(modsFolderPath, folderPath) ||
            !ModListDetector.IsManagedListFolder(folderPath))
        {
            throw new InvalidOperationException("Only recognized managed mod-list folders can be deleted.");
        }

        Directory.Delete(folderPath, recursive: true);
    }
}
