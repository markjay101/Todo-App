using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Todo_App.Domain.Entities;

namespace Todo_App.Infrastructure.Persistence.Configurations;
public class TodoItemTagConfiguration : IEntityTypeConfiguration<TodoItemTag>
{
    public void Configure(EntityTypeBuilder<TodoItemTag> builder)
    {
        builder.HasOne(tt => tt.Tag)
            .WithMany(t => t.TodoItemTags)
            .HasForeignKey(t => t.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tt => tt.TodoItem)
            .WithMany(t => t.TodoItemTags)
            .HasForeignKey(t => t.TodoItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
