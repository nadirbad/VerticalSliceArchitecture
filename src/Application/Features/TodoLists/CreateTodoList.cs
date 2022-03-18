using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Entities;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.Features.TodoLists;

public class CreateTodoListController : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<int>> Create(CreateTodoListCommand command)
    {
        return await Mediator.Send(command);
    }
}

public class CreateTodoListCommand : IRequest<int>
{
    public string? Title { get; set; }
}

public class CreateTodoListCommandValidator : AbstractValidator<CreateTodoListCommand>
{
    private readonly ApplicationDbContext _context;

    public CreateTodoListCommandValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.")
            .MustAsync(BeUniqueTitle).WithMessage("The specified title already exists.");
    }

    private Task<bool> BeUniqueTitle(string title, CancellationToken cancellationToken)
    {
        return _context.TodoLists
            .AllAsync(l => l.Title != title, cancellationToken);
    }
}

internal class CreateTodoListCommandHandler : IRequestHandler<CreateTodoListCommand, int>
{
    private readonly ApplicationDbContext _context;

    public CreateTodoListCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateTodoListCommand request, CancellationToken cancellationToken)
    {
        var entity = new TodoList { Title = request.Title };

        _context.TodoLists.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
