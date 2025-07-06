using FluentAssertions;
using NUnit.Framework;
using Todo_App.Application.Common.Exceptions;
using Todo_App.Application.Tags.Commands.CreateTag;
using Todo_App.Application.Tags.Commands.DeleteTag;
using Todo_App.Application.TodoItems.Commands.CreateTodoItem;
using Todo_App.Application.TodoLists.Commands.CreateTodoList;
using Todo_App.Domain.Entities;

namespace Todo_App.Application.IntegrationTests.Tags.Commands;

using static Testing;

public class DeleteTagTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireValidId()
    {
        var command = new DeleteTagCommand(99);

        await FluentActions.Invoking(() =>
            SendAsync(command)).Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShouldDeleteTag()
    {
        var userId = await RunAsDefaultUserAsync();

        var tagId = await SendAsync(new CreateTagCommand
        {
            Name = "Tag to Delete"
        });

        var command = new DeleteTagCommand(tagId);

        await SendAsync(command);

        var tag = await FindAsync<Tag>(tagId);

        tag.Should().BeNull();
    }

    [Test]
    public async Task ShouldDeleteTagAndRelatedTodoItemTags()
    {
        var userId = await RunAsDefaultUserAsync();

        // Create a tag
        var tagId = await SendAsync(new CreateTagCommand
        {
            Name = "Tag to Delete"
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

        // Assign tag to item (this would be done through the AssignTagsToTodoItem command)
        // For this test, we'll manually create the TodoItemTag relationship
        var todoItemTag = new TodoItemTag
        {
            TagId = tagId,
            TodoItemId = itemId
        };
        await AddAsync(todoItemTag);

        // Verify the relationship exists
        var tagCount = await CountAsync<TodoItemTag>();
        tagCount.Should().Be(1);

        // Delete the tag
        var command = new DeleteTagCommand(tagId);
        await SendAsync(command);

        // Verify tag is deleted
        var tag = await FindAsync<Tag>(tagId);
        tag.Should().BeNull();

        // Verify related TodoItemTag is also deleted (due to cascade delete)
        var remainingTagCount = await CountAsync<TodoItemTag>();
        remainingTagCount.Should().Be(0);
    }
}