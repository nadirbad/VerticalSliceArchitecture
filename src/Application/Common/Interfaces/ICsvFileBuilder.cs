using VerticalSliceArchitecture.Application.Features.TodoLists;

namespace VerticalSliceArchitecture.Application.Common.Interfaces;

public interface ICsvFileBuilder
{
    byte[] BuildTodoItemsFile(IEnumerable<TodoItemRecord> records);
}
