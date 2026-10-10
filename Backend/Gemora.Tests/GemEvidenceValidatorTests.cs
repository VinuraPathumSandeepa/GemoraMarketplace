using Gemora.Application.Services;
using Gemora.Domain.Entities;

namespace Gemora.Tests;

public class GemEvidenceValidatorTests
{
    private readonly GemEvidenceValidator _validator = new();

    // ============================================================
    // GEM-VAL-01
    // NORMAL CASE
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public void GEM_VAL_01_ValidCompleteEvidence_IsValidWithFourChecks()
    {
        // Arrange
        var listing = CreateValidListing();

        // Act
        var result = _validator.Validate(listing);

        // Assert
        Assert.True(result.IsValid);

        Assert.Empty(result.Issues);

        Assert.Empty(result.Warnings);

        Assert.Equal(
            4,
            result.ChecksPerformed.Count
        );
    }

    // ============================================================
    // GEM-VAL-02
    // INVALID CASE - MISSING TITLE
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public void GEM_VAL_02_MissingTitle_IsInvalid()
    {
        // Arrange
        var listing = CreateValidListing();

        listing.Title = "   ";

        // Act
        var result = _validator.Validate(listing);

        // Assert
        Assert.False(result.IsValid);

        Assert.Contains(
            "Listing title is missing.",
            result.Issues
        );
    }

    // ============================================================
    // GEM-VAL-03
    // INVALID CASE - MISSING GEM TYPE
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public void GEM_VAL_03_MissingGemType_IsInvalid()
    {
        // Arrange
        var listing = CreateValidListing();

        listing.GemType = string.Empty;

        // Act
        var result = _validator.Validate(listing);

        // Assert
        Assert.False(result.IsValid);

        Assert.Contains(
            "Declared gemstone type is missing.",
            result.Issues
        );
    }

    // ============================================================
    // GEM-VAL-04
    // BOUNDARY CASE - ZERO CARAT
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public void GEM_VAL_04_ZeroCaratWeight_IsInvalid()
    {
        // Arrange
        var listing = CreateValidListing();

        listing.CaratWeight = 0;

        // Act
        var result = _validator.Validate(listing);

        // Assert
        Assert.False(result.IsValid);

        Assert.Contains(
            "Carat weight must be greater than zero.",
            result.Issues
        );
    }

    // ============================================================
    // GEM-VAL-05
    // INVALID CASE - NEGATIVE PRICE
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public void GEM_VAL_05_NegativePrice_IsInvalid()
    {
        // Arrange
        var listing = CreateValidListing();

        listing.Price = -1;

        // Act
        var result = _validator.Validate(listing);

        // Assert
        Assert.False(result.IsValid);

        Assert.Contains(
            "Listing price must be greater than zero.",
            result.Issues
        );
    }

    // ============================================================
    // GEM-VAL-06
    // FAILURE CASE - MISSING GEM IMAGE
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public void GEM_VAL_06_MissingGemstoneImage_IsInvalid()
    {
        // Arrange
        var listing = CreateValidListing();

        listing.PrimaryImageUrl = null;

        // Act
        var result = _validator.Validate(listing);

        // Assert
        Assert.False(result.IsValid);

        Assert.Contains(
            "A gemstone image has not been provided.",
            result.Issues
        );
    }

    // ============================================================
    // GEM-VAL-07
    // WARNING CASE - CERTIFICATE NUMBER MISSING
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public void GEM_VAL_07_CertificateWithMissingNumber_AddsWarning()
    {
        // Arrange
        var listing = CreateValidListing();

        listing.CertificateNumber = null;

        // Act
        var result = _validator.Validate(listing);

        // Assert
        Assert.True(result.IsValid);

        Assert.Contains(
            "A certificate file was provided, but the certificate number is missing.",
            result.Warnings
        );
    }

    // ============================================================
    // GEM-VAL-08
    // WARNING CASE - CERTIFICATE AUTHORITY MISSING
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public void GEM_VAL_08_CertificateWithMissingAuthority_AddsWarning()
    {
        // Arrange
        var listing = CreateValidListing();

        listing.CertificateAuthority = null;

        // Act
        var result = _validator.Validate(listing);

        // Assert
        Assert.True(result.IsValid);

        Assert.Contains(
            "A certificate file was provided, but the certificate authority is missing.",
            result.Warnings
        );
    }

    // ============================================================
    // GEM-VAL-09
    // CERTIFICATE METADATA EXISTS BUT FILE IS MISSING
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public void GEM_VAL_09_CertificateMetadataWithoutFile_AddsWarnings()
    {
        // Arrange
        var listing = CreateValidListing();

        listing.CertificateUrl = null;

        // Act
        var result = _validator.Validate(listing);

        // Assert
        Assert.True(result.IsValid);

        Assert.Contains(
            "No gemstone certificate file has been provided.",
            result.Warnings
        );

        Assert.Contains(
            "Certificate information was entered, but no certificate file was uploaded.",
            result.Warnings
        );
    }

    // ============================================================
    // GEM-VAL-10
    // OPTIONAL CHARACTERISTICS CASE
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public void GEM_VAL_10_MissingCharacteristics_AddsThreeWarnings()
    {
        // Arrange
        var listing = CreateValidListing();

        listing.Color = string.Empty;

        listing.Clarity = string.Empty;

        listing.Cut = string.Empty;

        // Act
        var result = _validator.Validate(listing);

        // Assert
        Assert.True(result.IsValid);

        Assert.Contains(
            "Gemstone color has not been provided.",
            result.Warnings
        );

        Assert.Contains(
            "Gemstone clarity has not been provided.",
            result.Warnings
        );

        Assert.Contains(
            "Gemstone cut has not been provided.",
            result.Warnings
        );

        Assert.Equal(
            3,
            result.Warnings.Count
        );
    }

    // ============================================================
    // VALID GEM LISTING FIXTURE
    // ============================================================

    private static GemListing CreateValidListing()
    {
        return new GemListing
        {
            Title = "Blue Topaz",

            GemType = "Topaz",

            Description =
                "Natural blue topaz gemstone prepared for verification testing.",

            CaratWeight = 5.28m,

            Color = "Vivid Blue",

            Clarity = "Eye Clean",

            Cut = "Oval Mixed Cut",

            Price = 125000m,

            Currency = "LKR",

            PrimaryImageUrl =
                "https://test.gemora.local/images/blue-topaz.jpg",

            CertificateNumber =
                "GMT-BT-2026-014",

            CertificateAuthority =
                "Gemora Gem Laboratory",

            CertificateUrl =
                "https://test.gemora.local/certificates/blue-topaz.pdf"
        };
    }
}