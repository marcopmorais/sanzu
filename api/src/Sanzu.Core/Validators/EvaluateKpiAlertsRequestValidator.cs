using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class EvaluateKpiAlertsRequestValidator : AbstractValidator<EvaluateKpiAlertsRequest>
{
    public EvaluateKpiAlertsRequestValidator()
    {
        RuleFor(x => x.PeriodDays)
            .InclusiveBetween(7, 365);

        RuleFor(x => x.TenantLimit)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.CaseLimit)
            .InclusiveBetween(1, 100);
    }
}
