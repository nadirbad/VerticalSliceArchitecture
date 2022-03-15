using VerticalSliceArchitecture.Application.TodoLists.Queries.ExportTodos;

namespace VerticalSliceArchitecture.Application.Common.Interfaces;

public interface ICsvFileBuilder
{
    byte[] BuildTodoItemsFile(IEnumerable<TodoItemRecord> records);
}
