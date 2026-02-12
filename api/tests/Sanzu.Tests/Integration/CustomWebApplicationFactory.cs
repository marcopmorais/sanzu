using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Tests.Integration;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"sanzu-integration-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(
            services =>
            {
                var descriptors = services
                    .Where(
                        d =>
                            d.ServiceType == typeof(DbContextOptions<SanzuDbContext>)
                            || d.ServiceType == typeof(SanzuDbContext)
                            || (d.ServiceType.IsGenericType
                                && d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)
                                && d.ServiceType.GenericTypeArguments[0] == typeof(SanzuDbContext)))
                    .ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<SanzuDbContext>(
                    options => options.UseInMemoryDatabase(_databaseName));
            });
    }
}
