using VerticalSliceArchitecture.Application.Common.Interfaces;

namespace VerticalSliceArchitecture.Infrastructure.Services;

public class DateTimeService : IDateTime
{
    public DateTime Now => DateTime.Now;
}
