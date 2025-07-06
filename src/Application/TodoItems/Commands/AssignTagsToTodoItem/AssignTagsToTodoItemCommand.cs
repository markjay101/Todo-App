using MediatR;
using Microsoft.EntityFrameworkCore;
using Todo_App.Application.Common.Interfaces;
using Todo_App.Domain.Entities;

namespace Todo_App.Application.TodoItems.Commands.AssignTagsToTodoItem;

public class AssignTagsToTodoItemCommand : IRequest<bool>
{
    public int TodoItemId { get; set; }
    public List<int> TagIds { get; set; } = new List<int>();
}

public class AssignTagsToTodoItemCommandHandler : IRequestHandler<AssignTagsToTodoItemCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public AssignTagsToTodoItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(AssignTagsToTodoItemCommand request, CancellationToken cancellationToken)
    {
        // Get the todo item
        var todoItem = await _context.TodoItems
            .Include(t => t.TodoItemTags)
            .ThenInclude(tit => tit.Tag)
            .FirstOrDefaultAsync(t => t.Id == request.TodoItemId, cancellationToken);

        if (todoItem == null)
        {
            return false;
        }

        // Remove existing tag assignments
        var existingTodoItemTags = await _context.TodoItemTags
            .Where(tit => tit.TodoItemId == request.TodoItemId)
            .ToListAsync(cancellationToken);

        _context.TodoItemTags.RemoveRange(existingTodoItemTags);

        // Add new tag assignments
        if (request.TagIds.Any())
        {
            var newTodoItemTags = request.TagIds.Select(tagId => new TodoItemTag
            {
                TodoItemId = request.TodoItemId,
                TagId = tagId
            });

            await _context.TodoItemTags.AddRangeAsync(newTodoItemTags, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}