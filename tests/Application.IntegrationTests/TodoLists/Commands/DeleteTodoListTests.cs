using VerticalSliceArchitecture.Application.Common.Exceptions;
using VerticalSliceArchitecture.Application.TodoLists.Commands.CreateTodoList;
using VerticalSliceArchitecture.Application.TodoLists.Commands.DeleteTodoList;
using VerticalSliceArchitecture.Domain.Entities;
using FluentAssertions;
using NUnit.Framework;

namespace VerticalSliceArchitecture.Application.IntegrationTests.TodoLists.Commands;

using static Testing;

public class DeleteTodoListTests : TestBase
{
    [Test]
    public async Task ShouldRequireValidTodoListId()
    {
        var command = new DeleteTodoListCommand { Id = 99 };
        await FluentActions.Invoking(() => SendAsync(command)).Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShouldDeleteTodoList()
    {
        var listId = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });

        await SendAsync(new DeleteTodoListCommand
        {
            Id = listId
        });

        var list = await FindAsync<TodoList>(listId);

        list.Should().BeNull();
    }
}
