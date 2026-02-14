using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Tests.Integration.Controllers;

public sealed class PublicControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PublicControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SubmitDemoRequest_ShouldReturn201_AndPersistLead()
    {
        var client = _factory.CreateClient();
        var request = new SubmitDemoRequest
        {
            FullName = "Visitor Demo",
            Email = "demo.visitor@sanzu.test",
            OrganizationName = "Sanzu Prospect Org",
            TeamSize = 12,
            UtmSource = "linkedin",
            UtmMedium = "paid-social",
            UtmCampaign = "q1-launch",
            LandingPath = "/demo"
        };

        var response = await client.PostAsJsonAsync("/api/v1/public/demo-request", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<PublicLeadCaptureResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Qualified.Should().BeTrue();
        body.Data.RouteTarget.Should().Be("CRM");
        body.Data.RouteStatus.Should().Be("SUCCEEDED");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var lead = dbContext.PublicLeads.SingleOrDefault(x => x.Email == "demo.visitor@sanzu.test");
        lead.Should().NotBeNull();
        lead!.UtmSource.Should().Be("linkedin");
    }

    [Fact]
    public async Task StartAccountIntent_ShouldReturn201_AndRouteToOnboarding()
    {
        var client = _factory.CreateClient();
        var request = new StartAccountIntentRequest
        {
            AgencyName = "Horizon Family Partners",
            AdminFullName = "Agency Owner",
            AdminEmail = "owner@sanzu.test",
            Location = "Lisbon",
            TermsAccepted = true,
            UtmSource = "organic"
        };

        var response = await client.PostAsJsonAsync("/api/v1/public/start-account", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<PublicLeadCaptureResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Qualified.Should().BeTrue();
        body.Data.RouteTarget.Should().Be("ONBOARDING");
        body.Data.RouteStatus.Should().Be("SUCCEEDED");
    }

    [Fact]
    public async Task StartAccountIntent_ShouldReturn400_WhenTermsNotAccepted()
    {
        var client = _factory.CreateClient();
        var request = new StartAccountIntentRequest
        {
            AgencyName = "Horizon Family Partners",
            AdminFullName = "Agency Owner",
            AdminEmail = "owner@sanzu.test",
            Location = "Lisbon",
            TermsAccepted = false
        };

        var response = await client.PostAsJsonAsync("/api/v1/public/start-account", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        body.Should().NotBeNull();
        body!.Errors.Keys.Should().Contain(nameof(StartAccountIntentRequest.TermsAccepted));
    }
}
