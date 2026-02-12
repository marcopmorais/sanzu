using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sanzu.API.Middleware;
using Sanzu.Core.Entities;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Tests.Unit.Middleware;

public sealed class TenantContextMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldApplyTenantQueryFilter_WhenAuthenticatedTenantClaimExists()
    {
        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var orgA = BuildOrganization("Org A");
        var orgB = BuildOrganization("Org B");
        dbContext.Organizations.AddRange(orgA, orgB);
        await dbContext.SaveChangesAsync();

        var visibleOrganizationIds = new List<Guid>();
        var middleware = new TenantContextMiddleware(
            async _ =>
            {
                visibleOrganizationIds = await dbContext.Organizations
                    .Select(x => x.Id)
                    .ToListAsync();
            });

        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim("tenant_id", orgA.Id.ToString())
                    },
                    authenticationType: "TestAuth"))
        };

        await middleware.InvokeAsync(httpContext, dbContext);

        dbContext.CurrentOrganizationId.Should().Be(orgA.Id);
        visibleOrganizationIds.Should().ContainSingle().Which.Should().Be(orgA.Id);
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotApplyTenantQueryFilter_WhenPrincipalIsNotAuthenticated()
    {
        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        dbContext.Organizations.AddRange(BuildOrganization("Org A"), BuildOrganization("Org B"));
        await dbContext.SaveChangesAsync();

        var visibleCount = 0;
        var middleware = new TenantContextMiddleware(
            async _ =>
            {
                visibleCount = await dbContext.Organizations.CountAsync();
            });

        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim("tenant_id", Guid.NewGuid().ToString())
                    }))
        };

        await middleware.InvokeAsync(httpContext, dbContext);

        dbContext.CurrentOrganizationId.Should().BeNull();
        visibleCount.Should().Be(2);
    }

    private static Organization BuildOrganization(string name)
    {
        return new Organization
        {
            Id = Guid.NewGuid(),
            Name = name,
            Location = "Lisbon"
        };
    }

    private static AsyncServiceScope CreateScope()
    {
        var services = new ServiceCollection();
        services.AddDbContext<SanzuDbContext>(
            options => options.UseInMemoryDatabase($"tenant-context-tests-{Guid.NewGuid()}"));
        var provider = services.BuildServiceProvider();
        return provider.CreateAsyncScope();
    }
}
