using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class UpdateWorkflowTaskStatusRequestValidator : AbstractValidator<UpdateWorkflowTaskStatusRequest>
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "STARTED",
        "COMPLETED",
        "NEEDSREVIEW"
    };

    public UpdateWorkflowTaskStatusRequestValidator()
    {
        RuleFor(x => x.TargetStatus)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("TargetStatus is required.")
            .Must(status => AllowedStatuses.Contains(status.Trim()))
            .WithMessage("TargetStatus must be Started, Completed, or NeedsReview.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000);
    }
}
