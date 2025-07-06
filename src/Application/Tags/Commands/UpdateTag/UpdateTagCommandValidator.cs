using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Todo_App.Application.Common.Interfaces;

namespace Todo_App.Application.Tags.Commands.UpdateTag;
public class UpdateTagCommandValidator : AbstractValidator<UpdateTagCommand>
{
    private readonly IApplicationDbContext _context;
    public UpdateTagCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.Name)
            .MaximumLength(50)
            .NotEmpty()
            .MustAsync(BeUniqueName);
    }

    private async Task<bool> BeUniqueName(string name, CancellationToken cancellationToken)
    {
        return !await _context.Tags.AnyAsync(t => t.Name == name, cancellationToken);
    }
}
