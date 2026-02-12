using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface ITenantOnboardingService
{
    Task<CreateAgencyAccountResponse> CreateAgencyAccountAsync(
        CreateAgencyAccountRequest request,
        CancellationToken cancellationToken);

    Task<TenantOnboardingProfileResponse> UpdateOnboardingProfileAsync(
        Guid tenantId,
        Guid actorUserId,
        UpdateTenantOnboardingProfileRequest request,
        CancellationToken cancellationToken);

    Task<TenantOnboardingDefaultsResponse> UpdateOnboardingDefaultsAsync(
        Guid tenantId,
        Guid actorUserId,
        UpdateTenantOnboardingDefaultsRequest request,
        CancellationToken cancellationToken);

    Task<TenantInvitationResponse> CreateInvitationAsync(
        Guid tenantId,
        Guid actorUserId,
        CreateTenantInvitationRequest request,
        CancellationToken cancellationToken);

    Task<TenantOnboardingCompletionResponse> CompleteOnboardingAsync(
        Guid tenantId,
        Guid actorUserId,
        CompleteTenantOnboardingRequest request,
        CancellationToken cancellationToken);
}
