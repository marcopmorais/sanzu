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
            });

        services.AddScoped<ITenantOnboardingService, TenantOnboardingService>();
        services.AddScoped<IValidator<CreateAgencyAccountRequest>, CreateAgencyAccountRequestValidator>();
        services.AddScoped<IValidator<UpdateTenantOnboardingProfileRequest>, UpdateTenantOnboardingProfileRequestValidator>();
        services.AddScoped<IValidator<UpdateTenantOnboardingDefaultsRequest>, UpdateTenantOnboardingDefaultsRequestValidator>();
        services.AddScoped<IValidator<CreateTenantInvitationRequest>, CreateTenantInvitationRequestValidator>();
        services.AddScoped<IValidator<CompleteTenantOnboardingRequest>, CompleteTenantOnboardingRequestValidator>();

        return services;
    }
}
