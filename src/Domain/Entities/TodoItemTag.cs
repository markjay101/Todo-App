namespace Todo_App.Domain.Entities;
public class TodoItemTag
{
    public int Id { get; set; }
    public int TagId { get; set; }
    public int TodoItemId { get; set; }

    public Tag Tag { get; set; } = new Tag();
    public TodoItem TodoItem { get; set; } = new TodoItem();
}
