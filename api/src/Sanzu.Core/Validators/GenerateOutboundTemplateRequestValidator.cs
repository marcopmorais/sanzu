using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class GenerateOutboundTemplateRequestValidator : AbstractValidator<GenerateOutboundTemplateRequest>
{
    public GenerateOutboundTemplateRequestValidator()
    {
        RuleFor(x => x.TemplateKey)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("TemplateKey is required.")
            .MaximumLength(64);
    }
}
