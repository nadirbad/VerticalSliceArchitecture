using ErrorOr;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.Features.TodoLists;

public class DeleteTodoListController : ApiControllerBase
{
    [HttpDelete("/api/todo-lists/{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var result = await Mediator.Send(new DeleteTodoListCommand(id));

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}

public record DeleteTodoListCommand(int Id) : IRequest<ErrorOr<Success>>;

internal sealed class DeleteTodoListCommandHandler(ApplicationDbContext context) : IRequestHandler<DeleteTodoListCommand, ErrorOr<Success>>
{
    private readonly ApplicationDbContext _context = context;

    public async Task<ErrorOr<Success>> Handle(DeleteTodoListCommand request, CancellationToken cancellationToken)
    {
        var todoList = await _context.TodoLists
            .FindAsync([request.Id], cancellationToken);

        if (todoList is null)
        {
            return Error.NotFound(description: "TodoList not found.");
        }

        _context.TodoLists.Remove(todoList);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}