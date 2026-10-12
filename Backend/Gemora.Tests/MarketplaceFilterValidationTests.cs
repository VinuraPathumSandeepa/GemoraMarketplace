
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Gemora.Application.DTOs.Marketplace;
using Xunit;

namespace Gemora.Tests;

public class MarketplaceFilterValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void C2_02_PageSizeOutsideAllowedRange_IsInvalid(
        int pageSize)
    {
        // Arrange
        var query = new MarketplaceSearchQueryDto
        {
            Search = "Sapphire",
            Page = 1,
            PageSize = pageSize
        };

        var results = new List<ValidationResult>();

        // Act
        bool valid = Validator.TryValidateObject(
            query,
            new ValidationContext(query),
            results,
            true
        );

        // Assert
        Assert.False(valid);

        Assert.Contains(
            results,
            r => r.MemberNames.Contains(
                nameof(MarketplaceSearchQueryDto.PageSize)
            )
        );
    }
}
