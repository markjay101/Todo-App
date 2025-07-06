namespace Todo_App.Domain.Entities;
public class Tag : BaseAuditableEntity
{
    public string? Name { get; set; }

    public IList<TodoItemTag> TodoItemTags { get; set; } = new List<TodoItemTag>();
}
