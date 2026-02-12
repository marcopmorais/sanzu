using FluentAssertions;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Validators;

namespace Sanzu.Tests.Unit.Validators;

public sealed class CreateAgencyAccountRequestValidatorTests
{
    private readonly CreateAgencyAccountRequestValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldFail_WhenEmailInvalid()
    {
        var request = new CreateAgencyAccountRequest
        {
            Email = "invalid-email",
            FullName = "Agency Admin",
            AgencyName = "Agency",
            Location = "Lisbon"
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateAgencyAccountRequest.Email));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenRequiredFieldsMissing()
    {
        var request = new CreateAgencyAccountRequest
        {
            Email = string.Empty,
            FullName = string.Empty,
            AgencyName = string.Empty,
            Location = string.Empty
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateAgencyAccountRequest.Email));
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateAgencyAccountRequest.FullName));
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateAgencyAccountRequest.AgencyName));
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateAgencyAccountRequest.Location));
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenAllFieldsValid()
    {
        var request = new CreateAgencyAccountRequest
        {
            Email = "owner@agency.pt",
            FullName = "Agency Owner",
            AgencyName = "Lisbon Agency",
            Location = "Lisbon"
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }
}
