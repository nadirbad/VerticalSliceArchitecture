using ErrorOr;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Domain.Todos;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.Features.TodoItems;

public class DeleteTodoItemController : ApiControllerBase
{
    [HttpDelete("/api/todo-items/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await Mediator.Send(new DeleteTodoItemCommand(id));

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}

public record DeleteTodoItemCommand(int Id) : IRequest<ErrorOr<Success>>;

internal sealed class DeleteTodoItemCommandHandler(ApplicationDbContext context) : IRequestHandler<DeleteTodoItemCommand, ErrorOr<Success>>
{
    private readonly ApplicationDbContext _context = context;

    public async Task<ErrorOr<Success>> Handle(DeleteTodoItemCommand request, CancellationToken cancellationToken)
    {
        var todoItem = await _context.TodoItems
            .FindAsync([request.Id], cancellationToken);

        if (todoItem is null)
        {
            return Error.NotFound(description: "Todo item not found.");
        }

        _context.TodoItems.Remove(todoItem);

        todoItem.DomainEvents.Add(new TodoItemDeletedEvent(todoItem));

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}