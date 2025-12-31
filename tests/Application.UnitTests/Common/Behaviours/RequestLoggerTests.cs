using Microsoft.Extensions.Logging;

using Moq;

using VerticalSliceArchitecture.Application.Common.Behaviours;
using VerticalSliceArchitecture.Application.Common.Interfaces;
using VerticalSliceArchitecture.Application.Scheduling;

namespace VerticalSliceArchitecture.Application.UnitTests.Common.Behaviours;

public class RequestLoggerTests
{
    private readonly Mock<ILogger<BookAppointmentCommand>> _logger;
    private readonly Mock<ICurrentUserService> _currentUserService;

    public RequestLoggerTests()
    {
        _logger = new Mock<ILogger<BookAppointmentCommand>>();
        _currentUserService = new Mock<ICurrentUserService>();
    }

    [Fact]
    public async Task ShouldCallGetUserNameAsyncOnceIfAuthenticated()
    {
        _currentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());

        var requestLogger = new LoggingBehaviour<BookAppointmentCommand>(_logger.Object, _currentUserService.Object);

        var command = new BookAppointmentCommand(
            PatientId: Guid.NewGuid(),
            DoctorId: Guid.NewGuid(),
            Start: DateTimeOffset.UtcNow.AddDays(1),
            End: DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
            Notes: "Test appointment");

        await requestLogger.Process(command, CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotCallGetUserNameAsyncOnceIfUnauthenticated()
    {
        var requestLogger = new LoggingBehaviour<BookAppointmentCommand>(_logger.Object, _currentUserService.Object);

        var command = new BookAppointmentCommand(
            PatientId: Guid.NewGuid(),
            DoctorId: Guid.NewGuid(),
            Start: DateTimeOffset.UtcNow.AddDays(1),
            End: DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
            Notes: "Test appointment");

        await requestLogger.Process(command, CancellationToken.None);
    }
}