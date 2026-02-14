using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface IPublicConversionService
{
    Task<PublicLeadCaptureResponse> SubmitDemoRequestAsync(
        SubmitDemoRequest request,
        string? userAgent,
        string? clientIp,
        CancellationToken cancellationToken);

    Task<PublicLeadCaptureResponse> StartAccountIntentAsync(
        StartAccountIntentRequest request,
        string? userAgent,
        string? clientIp,
        CancellationToken cancellationToken);
}
