using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class ChangePlanRequestValidator : AbstractValidator<ChangePlanRequest>
{
    private static readonly HashSet<string> SupportedPlans = new(StringComparer.OrdinalIgnoreCase)
    {
        "STARTER",
        "GROWTH",
        "ENTERPRISE"
    };

    private static readonly HashSet<string> SupportedBillingCycles = new(StringComparer.OrdinalIgnoreCase)
    {
        "MONTHLY",
        "ANNUAL"
    };

    public ChangePlanRequestValidator()
    {
        RuleFor(x => x.PlanCode)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("PlanCode is required.")
            .MaximumLength(32)
            .Must(value => SupportedPlans.Contains(value.Trim()))
            .WithMessage("PlanCode is not supported.");

        RuleFor(x => x.BillingCycle)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("BillingCycle is required.")
            .MaximumLength(16)
            .Must(value => SupportedBillingCycles.Contains(value.Trim()))
            .WithMessage("BillingCycle is not supported.");

        // ConfirmedProrationAmount can be negative (downgrade credit) or positive (upgrade charge).
        // No range constraint — the service verifies it matches the calculated proration.
    }
}
