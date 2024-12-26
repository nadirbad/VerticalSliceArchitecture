using FluentValidation;
using FluentValidation.Results;

using MediatR;

using VerticalSliceArchitecture.Application.Common.Behaviours;
using VerticalSliceArchitecture.Application.Domain.Todos;
using VerticalSliceArchitecture.Application.Features.TodoLists;

namespace VerticalSliceArchitecture.Application.UnitTests.Common.Behaviours;

public class ValidationBehaviorTests
{
    private readonly ValidationBehavior<CreateTodoListCommand, ErrorOr<int>> _validationBehavior;
    private readonly IValidator<CreateTodoListCommand> _mockValidator;
    private readonly RequestHandlerDelegate<ErrorOr<int>> _mockNextBehavior;

    public ValidationBehaviorTests()
    {
        _mockNextBehavior = Substitute.For<RequestHandlerDelegate<ErrorOr<int>>>();
        _mockValidator = Substitute.For<IValidator<CreateTodoListCommand>>();

        _validationBehavior = new(_mockValidator);
    }

    [Fact]
    public async Task InvokeValidationBehavior_WhenValidatorResultIsValid_ShouldInvokeNextBehavior()
    {
        // Arrange
        var createTodoListCommand = new CreateTodoListCommand("Title");
        var todoList = new TodoList { Title = createTodoListCommand.Title };

        _mockValidator
            .ValidateAsync(createTodoListCommand, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mockNextBehavior.Invoke().Returns(todoList.Id);

        // Act
        var result = await _validationBehavior.Handle(createTodoListCommand, _mockNextBehavior, default);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(todoList.Id);
    }

    [Fact]
    public async Task InvokeValidationBehavior_WhenValidatorResultIsNotValid_ShouldReturnListOfErrors()
    {
        // Arrange
        var createTodoListCommand = new CreateTodoListCommand("Title");
        List<ValidationFailure> validationFailures = [new(propertyName: "foo", errorMessage: "bad foo")];

        _mockValidator
            .ValidateAsync(createTodoListCommand, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(validationFailures));

        // Act
        var result = await _validationBehavior.Handle(createTodoListCommand, _mockNextBehavior, default);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("foo");
        result.FirstError.Description.Should().Be("bad foo");
    }

    [Fact]
    public async Task InvokeValidationBehavior_WhenNoValidator_ShouldInvokeNextBehavior()
    {
        // Arrange
        var createTodoListCommand = new CreateTodoListCommand("Title");
        var validationBehavior = new ValidationBehavior<CreateTodoListCommand, ErrorOr<int>>();

        var todoList = new TodoList { Title = createTodoListCommand.Title };
        _mockNextBehavior.Invoke().Returns(todoList.Id);

        // Act
        var result = await validationBehavior.Handle(createTodoListCommand, _mockNextBehavior, default);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(todoList.Id);
    }
}