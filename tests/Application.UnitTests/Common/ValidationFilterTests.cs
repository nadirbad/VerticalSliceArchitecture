using FluentValidation;
using FluentValidation.Results;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;

using VerticalSliceArchitecture.Application.Common;

namespace VerticalSliceArchitecture.Application.UnitTests.Common;

public class ValidationFilterTests
{
    [Fact]
    public async Task InvokeAsync_WithNoValidator_CallsNextDelegate()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var context = CreateEndpointFilterContext(serviceProvider, new TestRequest { Name = "Test" });
        var nextCalled = false;
        var filter = new ValidationFilter<TestRequest>();

        ValueTask<object?> Next(EndpointFilterInvocationContext ctx)
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        }

        // Act
        await filter.InvokeAsync(context, Next);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithValidatorAndValidRequest_CallsNextDelegate()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<TestRequest>, TestRequestValidator>();
        var serviceProvider = services.BuildServiceProvider();

        var context = CreateEndpointFilterContext(serviceProvider, new TestRequest { Name = "ValidName" });
        var nextCalled = false;
        var filter = new ValidationFilter<TestRequest>();

        ValueTask<object?> Next(EndpointFilterInvocationContext ctx)
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        }

        // Act
        await filter.InvokeAsync(context, Next);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithValidatorAndInvalidRequest_ReturnsValidationProblem()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<TestRequest>, TestRequestValidator>();
        var serviceProvider = services.BuildServiceProvider();

        var context = CreateEndpointFilterContext(serviceProvider, new TestRequest { Name = string.Empty });
        var filter = new ValidationFilter<TestRequest>();

        static ValueTask<object?> Next(EndpointFilterInvocationContext ctx)
        {
            return ValueTask.FromResult<object?>(Results.Ok());
        }

        // Act
        var result = await filter.InvokeAsync(context, Next);

        // Assert
        result.Should().BeOfType<ValidationProblem>();
        var validationProblem = (ValidationProblem)result!;
        validationProblem.StatusCode.Should().Be(400);
        validationProblem.ProblemDetails.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public async Task InvokeAsync_WithMultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<TestRequest>, TestRequestValidatorWithMultipleRules>();
        var serviceProvider = services.BuildServiceProvider();

        var context = CreateEndpointFilterContext(serviceProvider, new TestRequest { Name = string.Empty, Age = -1 });
        var filter = new ValidationFilter<TestRequest>();

        static ValueTask<object?> Next(EndpointFilterInvocationContext ctx)
        {
            return ValueTask.FromResult<object?>(Results.Ok());
        }

        // Act
        var result = await filter.InvokeAsync(context, Next);

        // Assert
        result.Should().BeOfType<ValidationProblem>();
        var validationProblem = (ValidationProblem)result!;
        validationProblem.ProblemDetails.Errors.Should().ContainKey("Name");
        validationProblem.ProblemDetails.Errors.Should().ContainKey("Age");
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestNotInArguments_CallsNextWithoutValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<TestRequest>, TestRequestValidator>();
        var serviceProvider = services.BuildServiceProvider();

        // Create context without the request in arguments
        var context = CreateEndpointFilterContext(serviceProvider, argumentsToOmit: true);
        var nextCalled = false;
        var filter = new ValidationFilter<TestRequest>();

        ValueTask<object?> Next(EndpointFilterInvocationContext ctx)
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        }

        // Act
        await filter.InvokeAsync(context, Next);

        // Assert
        nextCalled.Should().BeTrue();
    }

    private static DefaultEndpointFilterInvocationContext CreateEndpointFilterContext(
        IServiceProvider serviceProvider,
        TestRequest? request = null,
        bool argumentsToOmit = false)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
        };

        var arguments = argumentsToOmit
            ? new List<object?>()
            : new List<object?> { request };

        return new DefaultEndpointFilterInvocationContext(httpContext, arguments.ToArray());
    }

    private record TestRequest
    {
        public string Name { get; init; } = string.Empty;
        public int Age { get; init; }
    }

    private class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        }
    }

    private class TestRequestValidatorWithMultipleRules : AbstractValidator<TestRequest>
    {
        public TestRequestValidatorWithMultipleRules()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
            RuleFor(x => x.Age).GreaterThan(0).WithMessage("Age must be greater than 0");
        }
    }

    // Helper class to create EndpointFilterInvocationContext
    private class DefaultEndpointFilterInvocationContext : EndpointFilterInvocationContext
    {
        private readonly HttpContext _httpContext;
        private readonly object?[] _arguments;

        public DefaultEndpointFilterInvocationContext(HttpContext httpContext, object?[] arguments)
        {
            _httpContext = httpContext;
            _arguments = arguments;
        }

        public override HttpContext HttpContext => _httpContext;

        public override IList<object?> Arguments => _arguments;

        public override T GetArgument<T>(int index)
        {
            if (index < 0 || index >= _arguments.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return (T)_arguments[index]!;
        }
    }
}