using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class OverrideWorkflowStepReadinessRequestValidator : AbstractValidator<OverrideWorkflowStepReadinessRequest>
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "READY",
        "BLOCKED"
    };

    public OverrideWorkflowStepReadinessRequestValidator()
    {
        RuleFor(x => x.TargetStatus)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("TargetStatus is required.")
            .Must(status => AllowedStatuses.Contains(status.Trim()))
            .WithMessage("TargetStatus must be either Ready or Blocked.");

        RuleFor(x => x.Rationale)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Rationale is required.")
            .MaximumLength(1000);
    }
}
