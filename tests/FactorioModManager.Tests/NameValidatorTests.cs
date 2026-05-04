using FactorioModManager.App.Factorio;

namespace FactorioModManager.Tests;

public sealed class NameValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("  name")]
    [InlineData(".hidden")]
    [InlineData("bad/name")]
    [InlineData("..")]
    [InlineData("pack..name")]
    public void Validate_rejects_unsafe_names(string name)
    {
        using var temp = new TempDirectory();

        var result = new NameValidator().Validate(name, temp.Path, []);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_rejects_duplicate_names_case_insensitively()
    {
        using var temp = new TempDirectory();

        var result = new NameValidator().Validate("spaceexploration", temp.Path, ["SpaceExploration"]);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_allows_current_name_when_renaming()
    {
        using var temp = new TempDirectory();

        var result = new NameValidator().Validate("SpaceExploration", temp.Path, ["SpaceExploration"], "SpaceExploration");

        Assert.True(result.IsValid);
    }
}
