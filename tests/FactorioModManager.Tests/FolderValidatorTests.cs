using FactorioModManager.App.Factorio;

namespace FactorioModManager.Tests;

public sealed class FolderValidatorTests
{
    [Fact]
    public void Validate_accepts_folder_with_mod_zip()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, "example_1.0.0.zip"), "not a real zip");

        var result = new FolderValidator().Validate(temp.Path);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_rejects_empty_folder()
    {
        using var temp = new TempDirectory();

        var result = new FolderValidator().Validate(temp.Path);

        Assert.False(result.IsValid);
    }
}
