using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;
using Sanzu.Infrastructure.Repositories;

namespace Sanzu.Infrastructure.DependencyInjection;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=Sanzu;Trusted_Connection=True;TrustServerCertificate=True";

        services.AddDbContext<SanzuDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<ITenantInvitationRepository, TenantInvitationRepository>();
        services.AddScoped<IBillingRecordRepository, BillingRecordRepository>();
        services.AddScoped<ICaseRepository, CaseRepository>();
        services.AddScoped<ICaseDocumentRepository, CaseDocumentRepository>();
        services.AddScoped<ICaseHandoffRepository, CaseHandoffRepository>();
        services.AddScoped<IProcessAliasRepository, ProcessAliasRepository>();
        services.AddScoped<IProcessEmailRepository, ProcessEmailRepository>();
        services.AddScoped<IExtractionCandidateRepository, ExtractionCandidateRepository>();
        services.AddScoped<ICaseParticipantRepository, CaseParticipantRepository>();
        services.AddScoped<IWorkflowStepRepository, WorkflowStepRepository>();
        services.AddScoped<ISupportDiagnosticSessionRepository, SupportDiagnosticSessionRepository>();
        services.AddScoped<ITenantPolicyControlRepository, TenantPolicyControlRepository>();
        services.AddScoped<IKpiThresholdRepository, KpiThresholdRepository>();
        services.AddScoped<IKpiAlertLogRepository, KpiAlertLogRepository>();
        services.AddScoped<ITenantInvitationNotificationSender, NoOpTenantInvitationNotificationSender>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
