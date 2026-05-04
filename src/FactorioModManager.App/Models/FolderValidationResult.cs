namespace FactorioModManager.App.Models;

public sealed class FolderValidationResult
{
    public required bool IsValid { get; init; }
    public required string Message { get; init; }
}
