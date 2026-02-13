using Microsoft.AspNetCore.Authentication;
using FluentValidation;
using Sanzu.API.Authentication;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Services;
using Sanzu.Core.Validators;
using Sanzu.Infrastructure.DependencyInjection;

namespace Sanzu.API.Configuration;

public static class ServiceRegistration
{
    public static IServiceCollection AddSanzuServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddInfrastructureServices(configuration);

        services
            .AddAuthentication(HeaderAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(
                HeaderAuthenticationHandler.SchemeName,
                _ => { });

        services.AddAuthorization(
            options =>
            {
                options.AddPolicy(
                    "TenantAdmin",
                    policy =>
                        policy
                            .AddAuthenticationSchemes(HeaderAuthenticationHandler.SchemeName)
                            .RequireAuthenticatedUser()
                            .RequireRole(nameof(PlatformRole.AgencyAdmin)));

                options.AddPolicy(
                    "SanzuAdmin",
                    policy =>
                        policy
                            .AddAuthenticationSchemes(HeaderAuthenticationHandler.SchemeName)
                            .RequireAuthenticatedUser()
                            .RequireRole(nameof(PlatformRole.SanzuAdmin)));
            });

        services.AddScoped<ITenantOnboardingService, TenantOnboardingService>();
        services.AddScoped<ITenantCaseDefaultsService, TenantCaseDefaultsService>();
        services.AddScoped<ITenantUsageIndicatorsService, TenantUsageIndicatorsService>();
        services.AddScoped<ITenantLifecycleService, TenantLifecycleService>();
        services.AddScoped<ISupportDiagnosticsService, SupportDiagnosticsService>();
        services.AddScoped<ITenantSubscriptionService, TenantSubscriptionService>();
        services.AddScoped<ITenantBillingService, TenantBillingService>();
        services.AddScoped<ICaseService, CaseService>();
        services.AddScoped<IValidator<CreateAgencyAccountRequest>, CreateAgencyAccountRequestValidator>();
        services.AddScoped<IValidator<CreateCaseRequest>, CreateCaseRequestValidator>();
        services.AddScoped<IValidator<SubmitCaseIntakeRequest>, SubmitCaseIntakeRequestValidator>();
        services.AddScoped<IValidator<ApplyExtractionDecisionsRequest>, ApplyExtractionDecisionsRequestValidator>();
        services.AddScoped<IValidator<GenerateOutboundTemplateRequest>, GenerateOutboundTemplateRequestValidator>();
        services.AddScoped<IValidator<UpdateCaseHandoffStateRequest>, UpdateCaseHandoffStateRequestValidator>();
        services.AddScoped<IValidator<UploadCaseDocumentRequest>, UploadCaseDocumentRequestValidator>();
        services.AddScoped<IValidator<UpdateCaseDocumentClassificationRequest>, UpdateCaseDocumentClassificationRequestValidator>();
        services.AddScoped<IValidator<OverrideWorkflowStepReadinessRequest>, OverrideWorkflowStepReadinessRequestValidator>();
        services.AddScoped<IValidator<UpdateWorkflowTaskStatusRequest>, UpdateWorkflowTaskStatusRequestValidator>();
        services.AddScoped<IValidator<UpdateCaseDetailsRequest>, UpdateCaseDetailsRequestValidator>();
        services.AddScoped<IValidator<UpdateCaseLifecycleRequest>, UpdateCaseLifecycleRequestValidator>();
        services.AddScoped<IValidator<InviteCaseParticipantRequest>, InviteCaseParticipantRequestValidator>();
        services.AddScoped<IValidator<AcceptCaseParticipantInvitationRequest>, AcceptCaseParticipantInvitationRequestValidator>();
        services.AddScoped<IValidator<UpdateCaseParticipantRoleRequest>, UpdateCaseParticipantRoleRequestValidator>();
        services.AddScoped<IValidator<UpdateTenantLifecycleStateRequest>, UpdateTenantLifecycleStateRequestValidator>();
        services.AddScoped<IValidator<StartSupportDiagnosticSessionRequest>, StartSupportDiagnosticSessionRequestValidator>();
        services.AddScoped<IValidator<UpdateTenantOnboardingProfileRequest>, UpdateTenantOnboardingProfileRequestValidator>();
        services.AddScoped<IValidator<UpdateTenantOnboardingDefaultsRequest>, UpdateTenantOnboardingDefaultsRequestValidator>();
        services.AddScoped<IValidator<UpdateTenantCaseDefaultsRequest>, UpdateTenantCaseDefaultsRequestValidator>();
        services.AddScoped<IValidator<CreateTenantInvitationRequest>, CreateTenantInvitationRequestValidator>();
        services.AddScoped<IValidator<CompleteTenantOnboardingRequest>, CompleteTenantOnboardingRequestValidator>();
        services.AddScoped<IValidator<ActivateTenantBillingRequest>, ActivateTenantBillingRequestValidator>();
        services.AddScoped<IValidator<PreviewPlanChangeRequest>, PreviewPlanChangeRequestValidator>();
        services.AddScoped<IValidator<ChangePlanRequest>, ChangePlanRequestValidator>();
        services.AddScoped<IValidator<CancelSubscriptionRequest>, CancelSubscriptionRequestValidator>();
        services.AddScoped<IValidator<RegisterFailedPaymentRequest>, RegisterFailedPaymentRequestValidator>();
        services.AddScoped<IValidator<ExecutePaymentRecoveryRequest>, ExecutePaymentRecoveryRequestValidator>();

        return services;
    }
}
