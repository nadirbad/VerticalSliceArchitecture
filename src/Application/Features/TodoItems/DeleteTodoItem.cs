﻿using MediatR;

using Microsoft.AspNetCore.Mvc;

using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Common.Exceptions;
using VerticalSliceArchitecture.Application.Entities;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.Features.TodoItems;

public class DeleteTodoItemController : ApiControllerBase
{
    [HttpDelete("/api/todo-items/{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        await Mediator.Send(new DeleteTodoItemCommand(id));

        return NoContent();
    }
}

public record DeleteTodoItemCommand(int Id) : IRequest;

internal sealed class DeleteTodoItemCommandHandler : IRequestHandler<DeleteTodoItemCommand>
{
    private readonly ApplicationDbContext _context;

    public DeleteTodoItemCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteTodoItemCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.TodoItems
            .FindAsync(new object[] { request.Id }, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(nameof(TodoItem), request.Id);
        }

        _context.TodoItems.Remove(entity);

        entity.DomainEvents.Add(new TodoItemDeletedEvent(entity));

        await _context.SaveChangesAsync(cancellationToken);
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