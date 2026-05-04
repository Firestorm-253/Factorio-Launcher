namespace FactorioModManager.App.Factorio;

public sealed class ActiveModListDetector
{
    public bool IsActive(string modsFolderPath, string modListFolderPath)
    {
        var rootModList = Path.Combine(modsFolderPath, FactorioFileNames.ModListJson);
        var rootSettings = Path.Combine(modsFolderPath, FactorioFileNames.ModSettingsDat);
        var listModList = Path.Combine(modListFolderPath, FactorioFileNames.ModListJson);
        var listSettings = Path.Combine(modListFolderPath, FactorioFileNames.ModSettingsDat);

        return FilesEqual(rootModList, listModList) && FilesEqual(rootSettings, listSettings);
    }

    private static bool FilesEqual(string left, string right)
    {
        if (!File.Exists(left) || !File.Exists(right))
        {
            return false;
        }

        var leftInfo = new FileInfo(left);
        var rightInfo = new FileInfo(right);
        if (leftInfo.Length != rightInfo.Length)
        {
            return false;
        }

        using var leftStream = File.OpenRead(left);
        using var rightStream = File.OpenRead(right);
        Span<byte> leftBuffer = stackalloc byte[8192];
        Span<byte> rightBuffer = stackalloc byte[8192];

        while (true)
        {
            var leftRead = leftStream.Read(leftBuffer);
            var rightRead = rightStream.Read(rightBuffer);
            if (leftRead != rightRead)
            {
                return false;
            }

            if (leftRead == 0)
            {
                return true;
            }

            if (!leftBuffer[..leftRead].SequenceEqual(rightBuffer[..rightRead]))
            {
                return false;
            }
        }
    }
}
