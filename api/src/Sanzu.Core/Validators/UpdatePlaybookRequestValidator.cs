using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class UpdatePlaybookRequestValidator : AbstractValidator<UpdatePlaybookRequest>
{
    public UpdatePlaybookRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(200)
            .When(x => x.Name is not null);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);

        RuleFor(x => x.ChangeNotes)
            .MaximumLength(2000)
            .When(x => x.ChangeNotes is not null);
    }
}
