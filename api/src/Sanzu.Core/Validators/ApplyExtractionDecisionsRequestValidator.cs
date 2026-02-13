using FluentValidation;
using Sanzu.Core.Enums;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class ApplyExtractionDecisionsRequestValidator : AbstractValidator<ApplyExtractionDecisionsRequest>
{
    public ApplyExtractionDecisionsRequestValidator()
    {
        RuleFor(x => x.Decisions)
            .NotEmpty()
            .WithMessage("At least one extraction decision is required.");

        RuleForEach(x => x.Decisions)
            .SetValidator(new ExtractionDecisionRequestValidator());
    }

    private sealed class ExtractionDecisionRequestValidator : AbstractValidator<ExtractionDecisionRequest>
    {
        public ExtractionDecisionRequestValidator()
        {
            RuleFor(x => x.CandidateId)
                .NotEmpty()
                .WithMessage("CandidateId is required.");

            RuleFor(x => x.Action)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Action is required.")
                .Must(BeSupportedAction)
                .WithMessage("Action must be Approve, Edit, or Reject.");

            RuleFor(x => x.EditedValue)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("EditedValue is required when action is Edit.")
                .MaximumLength(2048)
                .When(x => IsEditAction(x.Action));
        }

        private static bool BeSupportedAction(string value)
        {
            return Enum.TryParse<ExtractionDecisionAction>(value.Trim(), ignoreCase: true, out _);
        }

        private static bool IsEditAction(string value)
        {
            return Enum.TryParse<ExtractionDecisionAction>(value.Trim(), ignoreCase: true, out var action)
                   && action == ExtractionDecisionAction.Edit;
        }
    }
}
