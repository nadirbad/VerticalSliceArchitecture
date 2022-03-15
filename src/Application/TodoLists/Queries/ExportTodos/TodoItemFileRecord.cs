using VerticalSliceArchitecture.Application.Common.Mappings;
using VerticalSliceArchitecture.Domain.Entities;

namespace VerticalSliceArchitecture.Application.TodoLists.Queries.ExportTodos;

public class TodoItemRecord : IMapFrom<TodoItem>
{
    public string? Title { get; set; }

    public bool Done { get; set; }
}
