using ErrorOr;

using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Domain.Todos;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.Features.TodoLists;

public class GetTodosController : ApiControllerBase
{
    [HttpGet("/api/todo-lists")]
    public async Task<IActionResult> Get()
    {
        var result = await Mediator.Send(new GetTodosQuery());

        return result.Match(
            Ok,
            Problem);
    }
}

public record GetTodosQuery : IRequest<ErrorOr<TodosVm>>;

public class TodosVm
{
    public IList<PriorityLevelDto> PriorityLevels { get; set; } = [];

    public IList<TodoListDto> Lists { get; set; } = [];
}

public record PriorityLevelDto(int Value, string? Name);

public record TodoListDto(int Id, string? Title, string? Colour, IList<TodoItemDto> Items)
{
    public TodoListDto()
        : this(default, null, null, [])
    {
    }
}

public record TodoItemDto(int Id, int ListId, string? Title, bool Done, int Priority, string? Note);

internal sealed class GetTodosQueryHandler(ApplicationDbContext context) : IRequestHandler<GetTodosQuery, ErrorOr<TodosVm>>
{
    private readonly ApplicationDbContext _context = context;

    public async Task<ErrorOr<TodosVm>> Handle(GetTodosQuery request, CancellationToken cancellationToken)
    {
        return new TodosVm
        {
            PriorityLevels = [.. Enum.GetValues<PriorityLevel>().Select(p => new PriorityLevelDto((int)p, p.ToString()))],

            Lists = await _context.TodoLists
                .AsNoTracking()
                .OrderBy(t => t.Title)
                .Select(todoListItem => ToDto(todoListItem))
                .ToListAsync(cancellationToken),
        };
    }

    private static TodoItemDto ToDto(TodoItem todoItem)
    {
        var todoItemDto = new TodoItemDto(todoItem.Id, todoItem.ListId, todoItem.Title, todoItem.Done, (int)todoItem.Priority, todoItem.Note);

        return todoItemDto;
    }

    private static TodoListDto ToDto(TodoList todoList)
    {
        var todoListDto = new TodoListDto
        {
            Id = todoList.Id,
            Title = todoList.Title,
            Colour = todoList.Colour,
            Items = todoList.Items
                .Select(item => ToDto(item))
                .ToList(),
        };

        return todoListDto;
    }
}