using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using VerticalSliceArchitecture.Application.Common;

namespace VerticalSliceArchitecture.Application.UnitTests.Common;

public class MinimalApiProblemHelperTests
{
    [Fact]
    public void Problem_WithEmptyErrorList_ReturnsProblemWithStatus500()
    {
        // Arrange
        var errors = new List<Error>();

        // Act
        var result = MinimalApiProblemHelper.Problem(errors);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public void Problem_WithSingleValidationError_ReturnsValidationProblem()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.Validation("Field1", "Field1 is required"),
        };

        // Act
        var result = MinimalApiProblemHelper.Problem(errors);

        // Assert
        result.Should().BeOfType<ValidationProblem>();
        var validationResult = (ValidationProblem)result;
        validationResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        validationResult.ProblemDetails.Errors.Should().ContainKey("Field1");
        validationResult.ProblemDetails.Errors["Field1"].Should().Contain("Field1 is required");
    }

    [Fact]
    public void Problem_WithMultipleValidationErrors_ReturnsValidationProblemWithAllErrors()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.Validation("Field1", "Field1 is required"),
            Error.Validation("Field2", "Field2 must be a valid email"),
            Error.Validation("Field3", "Field3 must be between 1 and 100"),
        };

        // Act
        var result = MinimalApiProblemHelper.Problem(errors);

        // Assert
        result.Should().BeOfType<ValidationProblem>();
        var validationResult = (ValidationProblem)result;
        validationResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var problemDetails = validationResult.ProblemDetails;
        problemDetails.Errors.Should().NotBeNull();
        problemDetails.Errors.Should().HaveCount(3);
        problemDetails.Errors["Field1"].Should().Contain("Field1 is required");
        problemDetails.Errors["Field2"].Should().Contain("Field2 must be a valid email");
        problemDetails.Errors["Field3"].Should().Contain("Field3 must be between 1 and 100");
    }

    [Fact]
    public void Problem_WithConflictError_ReturnsProblemWithStatus409()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.Conflict("Appointment.Conflict", "The appointment time conflicts with an existing appointment"),
        };

        // Act
        var result = MinimalApiProblemHelper.Problem(errors);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        problemResult.ProblemDetails.Title.Should().Be("The appointment time conflicts with an existing appointment");
    }

    [Fact]
    public void Problem_WithNotFoundError_ReturnsProblemWithStatus404()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.NotFound("Appointment.NotFound", "Appointment not found"),
        };

        // Act
        var result = MinimalApiProblemHelper.Problem(errors);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        problemResult.ProblemDetails.Title.Should().Be("Appointment not found");
    }

    [Fact]
    public void Problem_WithUnauthorizedError_ReturnsProblemWithStatus403()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.Unauthorized("User.Unauthorized", "User is not authorized to perform this action"),
        };

        // Act
        var result = MinimalApiProblemHelper.Problem(errors);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        problemResult.ProblemDetails.Title.Should().Be("User is not authorized to perform this action");
    }

    [Fact]
    public void Problem_WithUnexpectedError_ReturnsProblemWithStatus500()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.Unexpected("Server.Error", "An unexpected error occurred"),
        };

        // Act
        var result = MinimalApiProblemHelper.Problem(errors);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        problemResult.ProblemDetails.Title.Should().Be("An unexpected error occurred");
    }

    [Fact]
    public void Problem_WithMixedErrorTypes_ReturnsProblemForFirstError()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.Validation("Field1", "Field1 is required"),
            Error.Conflict("Resource.Conflict", "Resource already exists"),
            Error.Validation("Field2", "Field2 is invalid"),
        };

        // Act
        var result = MinimalApiProblemHelper.Problem(errors);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        problemResult.ProblemDetails.Title.Should().Be("Field1 is required");
    }

    [Fact]
    public void Problem_WithFailureError_ReturnsProblemWithStatus500()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.Failure("Operation.Failed", "The operation failed to complete"),
        };

        // Act
        var result = MinimalApiProblemHelper.Problem(errors);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        problemResult.ProblemDetails.Title.Should().Be("The operation failed to complete");
    }
}