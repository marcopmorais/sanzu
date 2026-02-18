using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class CreatePlaybookRequestValidator : AbstractValidator<CreatePlaybookRequest>
{
    public CreatePlaybookRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);

        RuleFor(x => x.ChangeNotes)
            .MaximumLength(2000)
            .When(x => x.ChangeNotes is not null);
    }
}
