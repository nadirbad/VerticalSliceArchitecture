using ErrorOr;

using FluentValidation;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.Features.TodoLists;

public class UpdateTodoListController : ApiControllerBase
{
    [HttpPut("/api/todo-lists/{id}")]
    public async Task<IActionResult> Update(int id, UpdateTodoListCommand command)
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

public class UpdateTodoListCommand : IRequest<ErrorOr<Success>>
{
    public int Id { get; set; }

    public string? Title { get; set; }
}

public class UpdateTodoListCommandValidator : AbstractValidator<UpdateTodoListCommand>
{
    private readonly ApplicationDbContext _context;

    public UpdateTodoListCommandValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.")
            .MustAsync(BeUniqueTitle).WithMessage("The specified title already exists.");
    }

    public Task<bool> BeUniqueTitle(UpdateTodoListCommand model, string title, CancellationToken cancellationToken)
    {
        return _context.TodoLists
            .Where(l => l.Id != model.Id)
            .AllAsync(l => l.Title != title, cancellationToken);
    }
}

internal sealed class UpdateTodoListCommandHandler : IRequestHandler<UpdateTodoListCommand, ErrorOr<Success>>
{
    private readonly ApplicationDbContext _context;

    public UpdateTodoListCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ErrorOr<Success>> Handle(UpdateTodoListCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.TodoLists
            .FindAsync([request.Id], cancellationToken);

        if (entity is null)
        {
            return Error.NotFound(description: "TodoList not found.");
        }

        entity.Title = request.Title;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}