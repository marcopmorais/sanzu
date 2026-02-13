using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class UploadCaseDocumentRequestValidator : AbstractValidator<UploadCaseDocumentRequest>
{
    private const int MaxPayloadBytes = 10 * 1024 * 1024;

    public UploadCaseDocumentRequestValidator()
    {
        RuleFor(x => x.FileName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("FileName is required.")
            .MaximumLength(255);

        RuleFor(x => x.ContentType)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("ContentType is required.")
            .MaximumLength(127);

        RuleFor(x => x.ContentBase64)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("ContentBase64 is required.")
            .Must(IsValidBase64)
            .WithMessage("ContentBase64 must be valid Base64 data.")
            .Must(IsWithinMaxSize)
            .WithMessage("ContentBase64 exceeds the maximum supported size of 10MB.");
    }

    private static bool IsValidBase64(string value)
    {
        return Convert.TryFromBase64String(value.Trim(), new Span<byte>(new byte[value.Trim().Length]), out _);
    }

    private static bool IsWithinMaxSize(string value)
    {
        var normalized = value.Trim();
        try
        {
            var decodedLength = Convert.FromBase64String(normalized).LongLength;
            return decodedLength > 0 && decodedLength <= MaxPayloadBytes;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
