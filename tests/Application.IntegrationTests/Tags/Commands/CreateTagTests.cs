using FluentAssertions;
using NUnit.Framework;
using Todo_App.Application.Common.Exceptions;
using Todo_App.Application.Tags.Commands.CreateTag;
using Todo_App.Domain.Entities;

namespace Todo_App.Application.IntegrationTests.Tags.Commands;

using static Testing;

public class CreateTagTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireMinimumFields()
    {
        var command = new CreateTagCommand();

        await FluentActions.Invoking(() =>
            SendAsync(command)).Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldRequireUniqueName()
    {
        var userId = await RunAsDefaultUserAsync();

        var command1 = new CreateTagCommand
        {
            Name = "Test Tag"
        };

        var command2 = new CreateTagCommand
        {
            Name = "Test Tag"
        };

        await SendAsync(command1);

        await FluentActions.Invoking(() =>
            SendAsync(command2)).Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldCreateTag()
    {
        var userId = await RunAsDefaultUserAsync();

        var command = new CreateTagCommand
        {
            Name = "New Tag"
        };

        var tagId = await SendAsync(command);

        var tag = await FindAsync<Tag>(tagId);

        tag.Should().NotBeNull();
        tag!.Name.Should().Be(command.Name);
        tag.CreatedBy.Should().Be(userId);
        tag.Created.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMilliseconds(10000));
        tag.LastModifiedBy.Should().Be(userId);
        tag.LastModified.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMilliseconds(10000));
    }

    [Test]
    public async Task ShouldNotAllowEmptyName()
    {
        var command = new CreateTagCommand
        {
            Name = ""
        };

        await FluentActions.Invoking(() =>
            SendAsync(command)).Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldNotAllowNameTooLong()
    {
        var command = new CreateTagCommand
        {
            Name = new string('a', 51) // 51 characters, max is 50
        };

        await FluentActions.Invoking(() =>
            SendAsync(command)).Should().ThrowAsync<ValidationException>();
    }
}