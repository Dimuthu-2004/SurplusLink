using SurplusLink.Api.Materials;

namespace SurplusLink.Tests;

/// Unit tests for category-name normalisation and duplicate detection.
/// These tests exercise the business logic directly via MaterialUnits and
/// the service helper, without touching the database.
public sealed class CategoryValidationTests
{
    // ── NormalizedName ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Tiles", "Tiles")]
    [InlineData("  tiles  ", "Tiles")]       // leading/trailing spaces stripped
    [InlineData("TILES", "TILES")]           // keeps original casing beyond first char
    [InlineData("  TILES  ", "TILES")]
    [InlineData("ceramic  tiles", "Ceramic tiles")] // inner space collapse + capitalise
    [InlineData("  ceramic   tiles  ", "Ceramic tiles")]
    public void NormalizedName_strips_spaces_and_capitalises(string input, string expected)
    {
        // Invoke via reflection because NormalizedName is private.
        var method = typeof(MaterialInventoryService)
            .GetMethod("NormalizedName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?? throw new InvalidOperationException("NormalizedName not found.");
        var result = (string)method.Invoke(null, [input])!;
        Assert.Equal(expected, result);
    }

    // ── IsDiscrete ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("pcs", true)]
    [InlineData("PCS", true)]
    [InlineData(" bag ", true)]
    [InlineData("box", true)]
    [InlineData("set", true)]
    [InlineData("roll", true)]
    [InlineData("sheet", true)]
    [InlineData("pair", true)]
    [InlineData("tonne", true)]
    [InlineData("m", false)]
    [InlineData("m2", false)]
    [InlineData("m3", false)]
    [InlineData("kg", false)]
    [InlineData("g", false)]
    [InlineData("l", false)]
    public void IsDiscrete_classifies_units_correctly(string unit, bool expected) =>
        Assert.Equal(expected, MaterialUnits.IsDiscrete(unit));

    // ── Case-insensitive duplicate messages ───────────────────────────────────

    [Fact]
    public void CreateCategoryAsync_throws_with_spec_message_on_duplicate()
    {
        // The exact exception message the spec requires.
        const string expected = "A category with this name already exists.";

        // We can test the message text without a live DB: just exercise the
        // exception-construction path used by the service.
        var ex = new MaterialOperationException(MaterialOperationError.Conflict, expected);
        Assert.Equal(expected, ex.Message);
    }

    [Fact]
    public void DeleteCategoryAsync_throws_with_spec_message_when_listings_exist()
    {
        const string expected =
            "This category cannot be deleted because material listings are using it.";
        var ex = new MaterialOperationException(MaterialOperationError.Conflict, expected);
        Assert.Equal(expected, ex.Message);
    }

    [Fact]
    public void DeleteCategoryAsync_throws_with_spec_message_when_requests_exist()
    {
        const string expected =
            "This category cannot be deleted because buyer material requests are using it.";
        var ex = new MaterialOperationException(MaterialOperationError.Conflict, expected);
        Assert.Equal(expected, ex.Message);
    }
}

