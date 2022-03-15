using Microsoft.EntityFrameworkCore;
using VerticalSliceArchitecture.Application.Entities;

namespace VerticalSliceArchitecture.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<TodoList> TodoLists { get; }

    DbSet<TodoItem> TodoItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
