using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Tests.Integration.Controllers;

public sealed class TenantsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TenantsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Signup_ShouldReturn201_WhenValidInput()
    {
        var client = _factory.CreateClient();
        var request = BuildRequest("owner1@agency.pt");

        var response = await client.PostAsJsonAsync("/api/v1/tenants/signup", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<CreateAgencyAccountResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.TenantStatus.Should().Be(Core.Enums.TenantStatus.Pending);
    }

    [Fact]
    public async Task Signup_ShouldReturn400_WhenEmailInvalid()
    {
        var client = _factory.CreateClient();
        var request = BuildRequest("invalid-email");

        var response = await client.PostAsJsonAsync("/api/v1/tenants/signup", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        body.Should().NotBeNull();
        body!.Errors.Keys.Should().Contain(nameof(CreateAgencyAccountRequest.Email));
    }

    [Fact]
    public async Task Signup_ShouldReturn409_WhenEmailAlreadyExists()
    {
        var client = _factory.CreateClient();
        var request = BuildRequest("owner2@agency.pt");

        var firstResponse = await client.PostAsJsonAsync("/api/v1/tenants/signup", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondResponse = await client.PostAsJsonAsync("/api/v1/tenants/signup", request);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Signup_ShouldCreateAuditEvent_WhenAccountCreated()
    {
        var client = _factory.CreateClient();
        var request = BuildRequest("owner3@agency.pt");

        var response = await client.PostAsJsonAsync("/api/v1/tenants/signup", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<CreateAgencyAccountResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var auditEvent = dbContext.AuditEvents.SingleOrDefault(
            x => x.EventType == "TenantCreated" && x.ActorUserId == body.Data!.UserId);

        auditEvent.Should().NotBeNull();
    }

    private static CreateAgencyAccountRequest BuildRequest(string email)
    {
        return new CreateAgencyAccountRequest
        {
            Email = email,
            FullName = "Agency Owner",
            AgencyName = "Lisbon Agency",
            Location = "Lisbon"
        };
    }
}
