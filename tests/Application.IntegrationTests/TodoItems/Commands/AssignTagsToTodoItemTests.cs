using FluentAssertions;
using NUnit.Framework;
using Todo_App.Application.Tags.Commands.CreateTag;
using Todo_App.Application.TodoItems.Commands.AssignTagsToTodoItem;
using Todo_App.Application.TodoItems.Commands.CreateTodoItem;
using Todo_App.Application.TodoLists.Commands.CreateTodoList;
using Todo_App.Domain.Entities;

namespace Todo_App.Application.IntegrationTests.TodoItems.Commands;

using static Testing;

public class AssignTagsToTodoItemTests : BaseTestFixture
{
    [Test]
    public async Task ShouldAssignTagToTodoItem()
    {
        var userId = await RunAsDefaultUserAsync();

        // Create a tag
        var tagId = await SendAsync(new CreateTagCommand
        {
            Name = "Test Tag"
        });

        // Create a todo list and item
        var listId = await SendAsync(new CreateTodoListCommand
        {
            Title = "Test List"
        });

        var itemId = await SendAsync(new CreateTodoItemCommand
        {
            ListId = listId,
            Title = "Test Item"
        });

        var command = new AssignTagsToTodoItemCommand
        {
            TodoItemId = itemId,
            TagIds = new List<int> { tagId }
        };

        var result = await SendAsync(command);

        result.Should().BeTrue();

        // Verify the relationship was created
        var todoItemTagCount = await CountAsync<TodoItemTag>();
        todoItemTagCount.Should().Be(1);

        // Verify the relationship exists by checking the count
        // The relationship should be created in the database
        todoItemTagCount.Should().Be(1);
    }

    [Test]
    public async Task ShouldUnassignTagFromTodoItem()
    {
        var userId = await RunAsDefaultUserAsync();

        // Create a tag
        var tagId = await SendAsync(new CreateTagCommand
        {
            Name = "Test Tag"
        });

        // Create a todo list and item
        var listId = await SendAsync(new CreateTodoListCommand
        {
            Title = "Test List"
        });

        var itemId = await SendAsync(new CreateTodoItemCommand
        {
            ListId = listId,
            Title = "Test Item"
        });

        // First assign a tag
        var assignCommand = new AssignTagsToTodoItemCommand
        {
            TodoItemId = itemId,
            TagIds = new List<int> { tagId }
        };

        await SendAsync(assignCommand);

        // Verify tag was assigned
        var assignedCount = await CountAsync<TodoItemTag>();
        assignedCount.Should().Be(1);

        // Now unassign the tag (by providing empty list)
        var unassignCommand = new AssignTagsToTodoItemCommand
        {
            TodoItemId = itemId,
            TagIds = new List<int>()
        };

        await SendAsync(unassignCommand);

        // Verify tag was unassigned
        var unassignedCount = await CountAsync<TodoItemTag>();
        unassignedCount.Should().Be(0);
    }
}