using AutoMapper;
using Todo_App.Application.Common.Mappings;
using Todo_App.Application.Tags.Queries.GetTags;
using Todo_App.Domain.Entities;

namespace Todo_App.Application.TodoItems.Queries.GetTodoItemsWithPagination;

public class TodoItemBriefDto : IMapFrom<TodoItem>
{
    public int Id { get; set; }

    public int ListId { get; set; }

    public string? Title { get; set; }

    public bool Done { get; set; }

    public string? BackgroundColour { get; set; }

    public IList<TagDto> Tags { get; set; } = new List<TagDto>();

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TodoItem, TodoItemBriefDto>()
            .ForMember(d => d.BackgroundColour, opt => opt.MapFrom(s => s.BackgroundColour.Code))
            .ForMember(d => d.Tags, opt => opt.MapFrom(s => s.TodoItemTags.Select(tt => tt.Tag)));
    }
}
