using FluentAssertions;
using Sanzu.Core.Enums;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Validators;

namespace Sanzu.Tests.Unit.Validators;

public sealed class TenantOnboardingValidatorsTests
{
    [Fact]
    public async Task DefaultsValidator_ShouldFail_WhenValuesAreUnsupported()
    {
        var validator = new UpdateTenantOnboardingDefaultsRequestValidator();
        var request = new UpdateTenantOnboardingDefaultsRequest
        {
            DefaultLocale = "fr-FR",
            DefaultTimeZone = "Europe/Paris",
            DefaultCurrency = "GBP"
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task InviteValidator_ShouldFail_WhenRoleIsNotAgencyAdmin()
    {
        var validator = new CreateTenantInvitationRequestValidator();
        var request = new CreateTenantInvitationRequest
        {
            Email = "invitee@agency.pt",
            RoleType = PlatformRole.SanzuAdmin,
            ExpirationDays = 7
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateTenantInvitationRequest.RoleType));
    }

    [Fact]
    public async Task CompleteValidator_ShouldFail_WhenConfirmationIsFalse()
    {
        var validator = new CompleteTenantOnboardingRequestValidator();

        var result = await validator.ValidateAsync(new CompleteTenantOnboardingRequest { ConfirmCompletion = false });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ProfileValidator_ShouldPass_WhenPayloadIsValid()
    {
        var validator = new UpdateTenantOnboardingProfileRequestValidator();
        var request = new UpdateTenantOnboardingProfileRequest
        {
            AgencyName = "Agency",
            Location = "Lisbon"
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }
}
