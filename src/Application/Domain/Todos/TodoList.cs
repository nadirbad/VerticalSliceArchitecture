using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Domain.ValueObjects;

namespace VerticalSliceArchitecture.Application.Domain.Todos;

public class TodoList : AuditableEntity
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public Colour Colour { get; set; } = Colour.White;

    public IList<TodoItem> Items { get; private set; } = new List<TodoItem>();
}