using VerticalSliceArchitecture.Application.Common.Exceptions;
using VerticalSliceArchitecture.Application.Domain.ValueObjects;

namespace VerticalSliceArchitecture.Application.UnitTests.ValueObjects;

public class ColourTests
{
    [Fact]
    public void ShouldReturnCorrectColourCode()
    {
        var code = "#FFFFFF";

        var colour = Colour.From(code);

        colour.Code.Should().Be(code);
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
    public void ShouldPerformExplicitConversionGivenSupportedColourCode()
    {
        var colour = (Colour)"#FFFFFF";

        colour.Should().Be(Colour.White);
    }

    [Fact]
    public void ShouldThrowUnsupportedColourExceptionGivenNotSupportedColourCode()
    {
        FluentActions.Invoking(() => Colour.From("##FF33CC"))
            .Should().Throw<UnsupportedColourException>();
    }
}