using FluentAssertions;
using NUnit.Framework;
using Todo_App.Application.Tags.Commands.CreateTag;
using Todo_App.Application.Tags.Queries.GetTags;

namespace Todo_App.Application.IntegrationTests.Tags.Queries;

using static Testing;

public class GetTagsTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnEmptyListWhenNoTagsExist()
    {
        var query = new GetTagsQuery();

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task ShouldReturnAllTags()
    {
        var userId = await RunAsDefaultUserAsync();

        // Create multiple tags
        var tag1Id = await SendAsync(new CreateTagCommand
        {
            Name = "Tag 1"
        });

        var tag2Id = await SendAsync(new CreateTagCommand
        {
            Name = "Tag 2"
        });

        var tag3Id = await SendAsync(new CreateTagCommand
        {
            Name = "Tag 3"
        });

        var query = new GetTagsQuery();

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(t => t.Id == tag1Id && t.Name == "Tag 1");
        result.Should().Contain(t => t.Id == tag2Id && t.Name == "Tag 2");
        result.Should().Contain(t => t.Id == tag3Id && t.Name == "Tag 3");
    }

    [Test]
    public async Task ShouldReturnTagsInCorrectOrder()
    {
        var userId = await RunAsDefaultUserAsync();

        // Create tags in specific order
        var tag1Id = await SendAsync(new CreateTagCommand
        {
            Name = "First Tag"
        });

        var tag2Id = await SendAsync(new CreateTagCommand
        {
            Name = "Second Tag"
        });

        var tag3Id = await SendAsync(new CreateTagCommand
        {
            Name = "Third Tag"
        });

        var query = new GetTagsQuery();

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        // Tags should be returned in the order they were created (by ID)
        result[0].Id.Should().Be(tag1Id);
        result[1].Id.Should().Be(tag2Id);
        result[2].Id.Should().Be(tag3Id);
    }

    [Test]
    public async Task ShouldReturnCorrectTagProperties()
    {
        var userId = await RunAsDefaultUserAsync();

        var tagId = await SendAsync(new CreateTagCommand
        {
            Name = "Test Tag"
        });

        var query = new GetTagsQuery();

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);

        var tag = result[0];
        tag.Id.Should().Be(tagId);
        tag.Name.Should().Be("Test Tag");
    }
}