using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Sanzu.API.Authentication;

public sealed class HeaderAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "HeaderTenantAuth";

    public HeaderAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userIdHeader = Request.Headers["X-User-Id"].ToString();
        var tenantIdHeader = Request.Headers["X-Tenant-Id"].ToString();
        var roleHeader = Request.Headers["X-User-Role"].ToString();

        if (string.IsNullOrWhiteSpace(userIdHeader) || string.IsNullOrWhiteSpace(tenantIdHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!Guid.TryParse(userIdHeader, out var userId))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid X-User-Id header."));
        }

        if (!Guid.TryParse(tenantIdHeader, out var tenantId))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid X-Tenant-Id header."));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("tenant_id", tenantId.ToString()),
            new("org_id", tenantId.ToString())
        };

        if (!string.IsNullOrWhiteSpace(roleHeader))
        {
            claims.Add(new Claim(ClaimTypes.Role, roleHeader));
            claims.Add(new Claim("role", roleHeader));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
