namespace FactorioModManager.App.Models;

public sealed class ModListActivationResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? BackupFolderPath { get; init; }
    public bool RootFilesModified { get; init; }
}
