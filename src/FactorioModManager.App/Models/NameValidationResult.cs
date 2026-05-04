namespace FactorioModManager.App.Models;

public sealed class NameValidationResult
{
    public required bool IsValid { get; init; }
    public required string Message { get; init; }

    public static NameValidationResult Valid()
    {
        return new NameValidationResult
        {
            IsValid = true,
            Message = "Name is valid."
        };
    }

    public static NameValidationResult Invalid(string message)
    {
        return new NameValidationResult
        {
            IsValid = false,
            Message = message
        };
    }
}
