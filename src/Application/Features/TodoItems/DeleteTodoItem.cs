using MediatR;
using Microsoft.AspNetCore.Mvc;
using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Common.Exceptions;
using VerticalSliceArchitecture.Application.Common.Interfaces;
using VerticalSliceArchitecture.Application.Entities;

namespace VerticalSliceArchitecture.Application.Features.TodoItems;

public class DeleteTodoItemController : ApiControllerBase
{
    [HttpDelete("/todoItems/{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        await Mediator.Send(new DeleteTodoItemCommand {Id = id});

        return NoContent();
    }
}

public class DeleteTodoItemCommand : IRequest
{
    public int Id { get; set; }
}

internal class DeleteTodoItemCommandHandler : IRequestHandler<DeleteTodoItemCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteTodoItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(DeleteTodoItemCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.TodoItems
            .FindAsync(new object[] {request.Id}, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(nameof(TodoItem), request.Id);
        }

        _context.TodoItems.Remove(entity);

        entity.DomainEvents.Add(new TodoItemDeletedEvent(entity));

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public class TodoItemDeletedEvent : DomainEvent
{
    public TodoItemDeletedEvent(TodoItem item)
    {
        Item = item;
    }

    public TodoItem Item { get; }
}