using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using VerticalSliceArchitecture.Application.Domain.Todos;

namespace VerticalSliceArchitecture.Application.Features.TodoItems;

public class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.Property(t => t.Title)
            .HasMaxLength(200)
            .IsRequired();
    }
}