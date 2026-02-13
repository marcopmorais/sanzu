using FluentValidation;
using Sanzu.Core.Enums;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class UpdateCaseHandoffStateRequestValidator : AbstractValidator<UpdateCaseHandoffStateRequest>
{
    public UpdateCaseHandoffStateRequestValidator()
    {
        RuleFor(x => x.Status)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Status is required.")
            .Must(BeSupportedStatus)
            .WithMessage("Status must be PendingAdvisor, InProgress, Blocked, Completed, or Cancelled.");

        RuleFor(x => x.Notes)
            .MaximumLength(1024);
    }

    private static bool BeSupportedStatus(string value)
    {
        return Enum.TryParse<CaseHandoffStatus>(value.Trim(), ignoreCase: true, out _);
    }
}
