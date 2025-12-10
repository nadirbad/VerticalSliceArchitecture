using Microsoft.Extensions.Logging;

using Moq;

using VerticalSliceArchitecture.Application.Common.Behaviours;
using VerticalSliceArchitecture.Application.Common.Interfaces;
using VerticalSliceArchitecture.Application.Features.Healthcare;

namespace VerticalSliceArchitecture.Application.UnitTests.Common.Behaviours;

public class RequestLoggerTests
{
    private readonly Mock<ILogger<IssuePrescriptionCommand>> _logger;
    private readonly Mock<ICurrentUserService> _currentUserService;

    public RequestLoggerTests()
    {
        _logger = new Mock<ILogger<IssuePrescriptionCommand>>();
        _currentUserService = new Mock<ICurrentUserService>();
    }

    [Fact]
    public async Task ShouldCallGetUserNameAsyncOnceIfAuthenticated()
    {
        _currentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());

        var requestLogger = new LoggingBehaviour<IssuePrescriptionCommand>(_logger.Object, _currentUserService.Object);

        var command = new IssuePrescriptionCommand(
            PatientId: Guid.NewGuid(),
            DoctorId: Guid.NewGuid(),
            MedicationName: "Amoxicillin",
            Dosage: "500mg",
            Directions: "Take one capsule three times daily",
            Quantity: 30,
            NumberOfRefills: 2,
            DurationInDays: 10);

        await requestLogger.Process(command, CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotCallGetUserNameAsyncOnceIfUnauthenticated()
    {
        var requestLogger = new LoggingBehaviour<IssuePrescriptionCommand>(_logger.Object, _currentUserService.Object);

        var command = new IssuePrescriptionCommand(
            PatientId: Guid.NewGuid(),
            DoctorId: Guid.NewGuid(),
            MedicationName: "Amoxicillin",
            Dosage: "500mg",
            Directions: "Take one capsule three times daily",
            Quantity: 30,
            NumberOfRefills: 2,
            DurationInDays: 10);

        await requestLogger.Process(command, CancellationToken.None);
    }
}