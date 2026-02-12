using System.Security.Claims;
using Sanzu.Infrastructure.Data;

namespace Sanzu.API.Middleware;

public sealed class TenantContextMiddleware
{
    private static readonly string[] TenantClaimTypes =
    {
        "tenant_id",
        "org_id",
        "organization_id"
    };

    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, SanzuDbContext dbContext)
    {
        dbContext.CurrentOrganizationId = ResolveTenantId(context.User);
        await _next(context);
    }

    private static Guid? ResolveTenantId(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        foreach (var claimType in TenantClaimTypes)
        {
            var claimValue = principal.FindFirstValue(claimType);
            if (Guid.TryParse(claimValue, out var tenantId))
            {
                return tenantId;
            }
        }

        return null;
    }
}
