using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class UpdateTenantCaseDefaultsRequestValidator : AbstractValidator<UpdateTenantCaseDefaultsRequest>
{
    public UpdateTenantCaseDefaultsRequestValidator()
    {
        RuleFor(x => x)
            .Must(HasAtLeastOneOverride)
            .WithMessage("At least one case default field must be provided.");

        RuleFor(x => x.DefaultWorkflowKey)
            .Must(value => value is null || !string.IsNullOrWhiteSpace(value))
            .WithMessage("DefaultWorkflowKey cannot be empty when provided.")
            .MaximumLength(128);

        RuleFor(x => x.DefaultTemplateKey)
            .Must(value => value is null || !string.IsNullOrWhiteSpace(value))
            .WithMessage("DefaultTemplateKey cannot be empty when provided.")
            .MaximumLength(128);
    }

    private static bool HasAtLeastOneOverride(UpdateTenantCaseDefaultsRequest request)
    {
        return request.DefaultWorkflowKey is not null || request.DefaultTemplateKey is not null;
    }
}
