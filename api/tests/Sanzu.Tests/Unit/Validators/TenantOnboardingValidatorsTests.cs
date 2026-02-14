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

    [Fact]
    public async Task ActivateBillingValidator_ShouldFail_WhenPlanAndPaymentMethodAreUnsupported()
    {
        var validator = new ActivateTenantBillingRequestValidator();
        var request = new ActivateTenantBillingRequest
        {
            PlanCode = "BASIC",
            BillingCycle = "MONTHLY",
            PaymentMethodType = "CASH",
            PaymentMethodReference = "pm_123",
            InvoiceProfileLegalName = "Agency Lda",
            InvoiceProfileBillingEmail = "billing@agency.pt",
            InvoiceProfileCountryCode = "PT"
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(ActivateTenantBillingRequest.PlanCode));
        result.Errors.Should().Contain(x => x.PropertyName == nameof(ActivateTenantBillingRequest.PaymentMethodType));
    }

    [Fact]
    public async Task ActivateBillingValidator_ShouldPass_WhenPayloadIsValid()
    {
        var validator = new ActivateTenantBillingRequestValidator();
        var request = new ActivateTenantBillingRequest
        {
            PlanCode = "Starter",
            BillingCycle = "Monthly",
            PaymentMethodType = "Card",
            PaymentMethodReference = "pm_123",
            InvoiceProfileLegalName = "Agency Lda",
            InvoiceProfileVatNumber = "PT123456789",
            InvoiceProfileBillingEmail = "billing@agency.pt",
            InvoiceProfileCountryCode = "PT"
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CaseDefaultsValidator_ShouldFail_WhenNoFieldsAreProvided()
    {
        var validator = new UpdateTenantCaseDefaultsRequestValidator();

        var result = await validator.ValidateAsync(new UpdateTenantCaseDefaultsRequest());

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("At least one case default field"));
    }

    [Fact]
    public async Task CaseDefaultsValidator_ShouldPass_WhenWorkflowOrTemplateProvided()
    {
        var validator = new UpdateTenantCaseDefaultsRequestValidator();
        var request = new UpdateTenantCaseDefaultsRequest
        {
            DefaultWorkflowKey = "workflow.v3"
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }
}
