using Microsoft.EntityFrameworkCore;
using VerticalSliceArchitecture.Application.Domain.Todos;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;
using Xunit;

namespace VerticalSliceArchitecture.Application.UnitTests.Infrastructure;

public class EntityConfigurationTests
{
    [Fact]
    public void TodoItem_Configuration_Should_Be_Applied()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options, null!, null!, null!);

        // Act
        var entityType = context.Model.FindEntityType(typeof(TodoItem));

        // Assert
        Assert.NotNull(entityType);

        var titleProperty = entityType.FindProperty("Title");
        Assert.NotNull(titleProperty);
        Assert.Equal(200, titleProperty.GetMaxLength());
        Assert.False(titleProperty.IsNullable);
    }

    [Fact]
    public void TodoList_Configuration_Should_Be_Applied()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options, null!, null!, null!);

        // Act
        var entityType = context.Model.FindEntityType(typeof(TodoList));

        // Assert
        Assert.NotNull(entityType);

        var titleProperty = entityType.FindProperty("Title");
        Assert.NotNull(titleProperty);
        Assert.Equal(200, titleProperty.GetMaxLength());
        Assert.False(titleProperty.IsNullable);
    }
}