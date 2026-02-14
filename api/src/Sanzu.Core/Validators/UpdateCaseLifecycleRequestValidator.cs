using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class UpdateCaseLifecycleRequestValidator : AbstractValidator<UpdateCaseLifecycleRequest>
{
    private static readonly HashSet<string> AllowedTargetStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "INTAKE",
        "ACTIVE",
        "REVIEW",
        "CLOSED",
        "ARCHIVED"
    };

    public UpdateCaseLifecycleRequestValidator()
    {
        RuleFor(x => x.TargetStatus)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("TargetStatus is required.")
            .MaximumLength(32)
            .Must(status => AllowedTargetStatuses.Contains(status.Trim()))
            .WithMessage("TargetStatus is not supported for lifecycle updates.");

        RuleFor(x => x.Reason)
            .MaximumLength(2000)
            .When(x => x.Reason is not null);
    }
}
