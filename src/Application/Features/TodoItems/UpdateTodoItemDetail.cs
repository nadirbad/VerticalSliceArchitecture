using MediatR;
using Microsoft.AspNetCore.Mvc;
using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Common.Exceptions;
using VerticalSliceArchitecture.Application.Common.Interfaces;
using VerticalSliceArchitecture.Application.Entities;

namespace VerticalSliceArchitecture.Application.Features.TodoItems;

public class UpdateTodoItemDetailController : ApiControllerBase
{
    [HttpPut("/todoItems/[action]")]
    public async Task<ActionResult> UpdateItemDetails(int id, UpdateTodoItemDetailCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest();
        }

        await Mediator.Send(command);

        return NoContent();
    }
}

public class UpdateTodoItemDetailCommand : IRequest
{
    public int Id { get; set; }

    public int ListId { get; set; }

    public PriorityLevel Priority { get; set; }

    public string? Note { get; set; }
}

internal class UpdateTodoItemDetailCommandHandler : IRequestHandler<UpdateTodoItemDetailCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateTodoItemDetailCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(UpdateTodoItemDetailCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.TodoItems
            .FindAsync(new object[] {request.Id}, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(nameof(TodoItem), request.Id);
        }

        entity.ListId = request.ListId;
        entity.Priority = request.Priority;
        entity.Note = request.Note;

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}