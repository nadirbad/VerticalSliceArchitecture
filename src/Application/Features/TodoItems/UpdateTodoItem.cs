using ErrorOr;

using FluentValidation;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.Features.TodoItems;

public class TodoItemsController : ApiControllerBase
{
    [HttpPut("/api/todo-items/{id}")]
    public async Task<IActionResult> Update(int id, UpdateTodoItemCommand command)
    {
        if (id != command.Id)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: "Not matching ids");
        }

        var result = await Mediator.Send(command);

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}

public record UpdateTodoItemCommand(int Id, string? Title, bool Done) : IRequest<ErrorOr<Success>>;

internal sealed class UpdateTodoItemCommandValidator : AbstractValidator<UpdateTodoItemCommand>
{
    public UpdateTodoItemCommandValidator()
    {
        RuleFor(v => v.Title)
            .MaximumLength(200)
            .NotEmpty();
    }
}

internal sealed class UpdateTodoItemCommandHandler(ApplicationDbContext context) : IRequestHandler<UpdateTodoItemCommand, ErrorOr<Success>>
{
    private readonly ApplicationDbContext _context = context;

    public async Task<ErrorOr<Success>> Handle(UpdateTodoItemCommand request, CancellationToken cancellationToken)
    {
        var todoItem = await _context.TodoItems
            .FindAsync([request.Id], cancellationToken);

        if (todoItem is null)
        {
            return Error.NotFound(description: "Todo item not found.");
        }

        todoItem.Title = request.Title;
        todoItem.Done = request.Done;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}