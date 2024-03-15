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
    public async Task<ActionResult<TodosVm>> Get()
    {
        return await Mediator.Send(new GetTodosQuery());
    }
}

public record GetTodosQuery : IRequest<TodosVm>;

public class TodosVm
{
    public IList<PriorityLevelDto> PriorityLevels { get; set; } = new List<PriorityLevelDto>();

    public IList<TodoListDto> Lists { get; set; } = new List<TodoListDto>();
}

public record PriorityLevelDto(int Value, string? Name);

public record TodoListDto(int Id, string? Title, string? Colour, IList<TodoItemDto> Items)
{
    public TodoListDto()
        : this(default, null, null, new List<TodoItemDto>())
    {
    }
}

public record TodoItemDto(int Id, int ListId, string? Title, bool Done, int Priority, string? Note);

internal sealed class GetTodosQueryHandler(ApplicationDbContext context) : IRequestHandler<GetTodosQuery, TodosVm>
{
    private readonly ApplicationDbContext _context = context;

    public async Task<TodosVm> Handle(GetTodosQuery request, CancellationToken cancellationToken)
    {
        return new TodosVm
        {
            PriorityLevels = Enum.GetValues(typeof(PriorityLevel))
                .Cast<PriorityLevel>()
                .Select(p => new PriorityLevelDto((int)p, p.ToString()))
                .ToList(),

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