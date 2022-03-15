namespace VerticalSliceArchitecture.Application.Common.Exceptions;

public class UnsupportedColourException : Exception
{
    public UnsupportedColourException(string code)
        : base($"Colour '{code}' is unsupported.")
    {
    }
}
