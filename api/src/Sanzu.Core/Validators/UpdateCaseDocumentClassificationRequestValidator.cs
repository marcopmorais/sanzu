using FluentValidation;
using Sanzu.Core.Enums;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class UpdateCaseDocumentClassificationRequestValidator : AbstractValidator<UpdateCaseDocumentClassificationRequest>
{
    public UpdateCaseDocumentClassificationRequestValidator()
    {
        RuleFor(x => x.Classification)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Classification is required.")
            .Must(BeSupportedClassification)
            .WithMessage("Classification must be Required, Optional, or Restricted.");
    }

    private static bool BeSupportedClassification(string value)
    {
        return Enum.TryParse<CaseDocumentClassification>(value.Trim(), ignoreCase: true, out _);
    }
}
