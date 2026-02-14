using FluentValidation;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class PublicConversionService : IPublicConversionService
{
    private readonly IPublicLeadRepository _publicLeadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<SubmitDemoRequest> _submitDemoRequestValidator;
    private readonly IValidator<StartAccountIntentRequest> _startAccountIntentRequestValidator;

    public PublicConversionService(
        IPublicLeadRepository publicLeadRepository,
        IUnitOfWork unitOfWork,
        IValidator<SubmitDemoRequest> submitDemoRequestValidator,
        IValidator<StartAccountIntentRequest> startAccountIntentRequestValidator)
    {
        _publicLeadRepository = publicLeadRepository;
        _unitOfWork = unitOfWork;
        _submitDemoRequestValidator = submitDemoRequestValidator;
        _startAccountIntentRequestValidator = startAccountIntentRequestValidator;
    }

    public async Task<PublicLeadCaptureResponse> SubmitDemoRequestAsync(
        SubmitDemoRequest request,
        string? userAgent,
        string? clientIp,
        CancellationToken cancellationToken)
    {
        var validationResult = await _submitDemoRequestValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        return await CaptureAndRouteAsync(
            intentType: "DEMO_REQUEST",
            fullName: request.FullName,
            email: request.Email,
            organizationName: request.OrganizationName,
            teamSize: request.TeamSize,
            termsAccepted: false,
            utmSource: request.UtmSource,
            utmMedium: request.UtmMedium,
            utmCampaign: request.UtmCampaign,
            referrerPath: request.ReferrerPath,
            landingPath: request.LandingPath,
            userAgent: userAgent,
            clientIp: clientIp,
            cancellationToken: cancellationToken);
    }

    public async Task<PublicLeadCaptureResponse> StartAccountIntentAsync(
        StartAccountIntentRequest request,
        string? userAgent,
        string? clientIp,
        CancellationToken cancellationToken)
    {
        var validationResult = await _startAccountIntentRequestValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        return await CaptureAndRouteAsync(
            intentType: "START_ACCOUNT",
            fullName: request.AdminFullName,
            email: request.AdminEmail,
            organizationName: request.AgencyName,
            teamSize: 1,
            termsAccepted: request.TermsAccepted,
            utmSource: request.UtmSource,
            utmMedium: request.UtmMedium,
            utmCampaign: request.UtmCampaign,
            referrerPath: request.ReferrerPath,
            landingPath: request.LandingPath,
            userAgent: userAgent,
            clientIp: clientIp,
            cancellationToken: cancellationToken);
    }

    private async Task<PublicLeadCaptureResponse> CaptureAndRouteAsync(
        string intentType,
        string fullName,
        string email,
        string organizationName,
        int teamSize,
        bool termsAccepted,
        string? utmSource,
        string? utmMedium,
        string? utmCampaign,
        string? referrerPath,
        string? landingPath,
        string? userAgent,
        string? clientIp,
        CancellationToken cancellationToken)
    {
        PublicLead? lead = null;
        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var normalizedIntent = intentType.Trim().ToUpperInvariant();
                var normalizedEmail = email.Trim().ToLowerInvariant();
                var normalizedName = fullName.Trim();
                var normalizedOrg = organizationName.Trim();
                var qualified = IsQualified(normalizedIntent, teamSize);
                var routeTarget = DetermineRouteTarget(normalizedIntent, qualified);
                var routeResult = ExecuteRoute(routeTarget, normalizedEmail, normalizedOrg);
                var nowUtc = DateTime.UtcNow;

                lead = new PublicLead
                {
                    Id = Guid.NewGuid(),
                    IntentType = normalizedIntent,
                    FullName = normalizedName,
                    Email = normalizedEmail,
                    OrganizationName = normalizedOrg,
                    TeamSize = teamSize,
                    TermsAccepted = termsAccepted,
                    Qualified = qualified,
                    RouteTarget = routeTarget,
                    RouteStatus = routeResult.Status,
                    RouteFailureReason = routeResult.FailureReason,
                    UtmSource = NormalizeOptional(utmSource),
                    UtmMedium = NormalizeOptional(utmMedium),
                    UtmCampaign = NormalizeOptional(utmCampaign),
                    ReferrerPath = NormalizeOptional(referrerPath),
                    LandingPath = NormalizeOptional(landingPath),
                    UserAgent = NormalizeOptional(userAgent),
                    ClientIp = NormalizeOptional(clientIp),
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc
                };

                await _publicLeadRepository.CreateAsync(lead, token);
            },
            cancellationToken);

        return new PublicLeadCaptureResponse
        {
            LeadId = lead!.Id,
            IntentType = lead.IntentType,
            Qualified = lead.Qualified,
            RouteTarget = lead.RouteTarget,
            RouteStatus = lead.RouteStatus,
            RouteFailureReason = lead.RouteFailureReason,
            CapturedAt = lead.CreatedAt
        };
    }

    private static bool IsQualified(string intentType, int teamSize)
    {
        if (intentType == "START_ACCOUNT")
        {
            return true;
        }

        return teamSize >= 5;
    }

    private static string DetermineRouteTarget(string intentType, bool qualified)
    {
        if (!qualified)
        {
            return "NURTURE_QUEUE";
        }

        return intentType == "START_ACCOUNT" ? "ONBOARDING" : "CRM";
    }

    private static (string Status, string? FailureReason) ExecuteRoute(
        string routeTarget,
        string normalizedEmail,
        string normalizedOrganizationName)
    {
        if (routeTarget == "NURTURE_QUEUE")
        {
            return ("SKIPPED_NOT_QUALIFIED", null);
        }

        // Deterministic test hook to exercise failure logging paths.
        if (normalizedEmail.Contains("+fail-route@", StringComparison.Ordinal)
            || normalizedOrganizationName.Contains("fail-route", StringComparison.OrdinalIgnoreCase))
        {
            return ("FAILED", "SimulatedRoutingFailure");
        }

        return ("SUCCEEDED", null);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
