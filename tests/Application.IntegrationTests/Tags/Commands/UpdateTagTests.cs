using FluentAssertions;
using NUnit.Framework;
using Todo_App.Application.Common.Exceptions;
using Todo_App.Application.Tags.Commands.CreateTag;
using Todo_App.Application.Tags.Commands.UpdateTag;
using Todo_App.Domain.Entities;

namespace Todo_App.Application.IntegrationTests.Tags.Commands;

using static Testing;

public class UpdateTagTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireValidId()
    {
        var command = new UpdateTagCommand
        {
            Id = 99,
            Name = "Updated Tag"
        };

        await FluentActions.Invoking(() =>
            SendAsync(command)).Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShouldRequireUniqueName()
    {
        var userId = await RunAsDefaultUserAsync();

        var tag1Id = await SendAsync(new CreateTagCommand
        {
            Name = "First Tag"
        });

        var tag2Id = await SendAsync(new CreateTagCommand
        {
            Name = "Second Tag"
        });

        var command = new UpdateTagCommand
        {
            Id = tag2Id,
            Name = "First Tag" // Try to use the same name as tag1
        };

        await FluentActions.Invoking(() =>
            SendAsync(command)).Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldUpdateTag()
    {
        var userId = await RunAsDefaultUserAsync();

        var tagId = await SendAsync(new CreateTagCommand
        {
            Name = "Original Tag"
        });

        var command = new UpdateTagCommand
        {
            Id = tagId,
            Name = "Updated Tag"
        };

        await SendAsync(command);

        var tag = await FindAsync<Tag>(tagId);

        tag.Should().NotBeNull();
        tag!.Name.Should().Be("Updated Tag");
        tag.LastModifiedBy.Should().Be(userId);
        tag.LastModified.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMilliseconds(10000));
    }

    [Test]
    public async Task ShouldNotAllowEmptyName()
    {
        var userId = await RunAsDefaultUserAsync();

        var tagId = await SendAsync(new CreateTagCommand
        {
            Name = "Original Tag"
        });

        var command = new UpdateTagCommand
        {
            Id = tagId,
            Name = ""
        };

        await FluentActions.Invoking(() =>
            SendAsync(command)).Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldNotAllowNameTooLong()
    {
        var userId = await RunAsDefaultUserAsync();

        var tagId = await SendAsync(new CreateTagCommand
        {
            Name = "Original Tag"
        });

        var command = new UpdateTagCommand
        {
            Id = tagId,
            Name = new string('a', 51) // 51 characters, max is 50
        };

        await FluentActions.Invoking(() =>
            SendAsync(command)).Should().ThrowAsync<ValidationException>();
    }
}