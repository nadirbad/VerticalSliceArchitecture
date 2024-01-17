using VerticalSliceArchitecture.Application.Domain.Todos;

namespace VerticalSliceArchitecture.Application.Common.Interfaces;

public interface ICsvFileBuilder
{
    byte[] BuildTodoItemsFile(IEnumerable<TodoItemRecord> records);
}