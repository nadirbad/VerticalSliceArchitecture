using VerticalSliceArchitecture.Application.Domain.ValueObjects;

namespace VerticalSliceArchitecture.Application.UnitTests.ValueObjects;

public class ColourTests
{
    [Fact]
    public void ShouldReturnCorrectColourCode()
    {
        var code = "#FFFFFF";

        var colour = Colour.From(code);

        colour.IsError.Should().BeFalse();
        colour.Value.Code.Should().Be(code);
    }

    [Fact]
    public void ToStringReturnsCode()
    {
        var colour = Colour.White;

        colour.ToString().Should().Be(colour.Code);
    }

    [Fact]
    public void ShouldPerformImplicitConversionToColourCodeString()
    {
        string code = Colour.White;

        code.Should().Be("#FFFFFF");
    }

    [Fact]
    public void ShouldReturnErrorGivenNotSupportedColourCode()
    {
        // Arrange/Act
        var colour = Colour.From("##FF33CC");

        // Assert
        colour.IsError.Should().BeTrue();
        colour.FirstError.Code.Should().Be("ColourErrors.UnsupportedColour");
    }
}