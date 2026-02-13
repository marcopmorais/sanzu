using FluentAssertions;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Validators;

namespace Sanzu.Tests.Unit.Validators;

public sealed class TenantCaseDefaultsValidatorTests
{
    [Fact]
    public async Task Validator_ShouldFail_WhenNoFieldsProvided()
    {
        var validator = new UpdateTenantCaseDefaultsRequestValidator();

        var result = await validator.ValidateAsync(new UpdateTenantCaseDefaultsRequest());

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_ShouldPass_WhenAtLeastOneFieldProvided()
    {
        var validator = new UpdateTenantCaseDefaultsRequestValidator();
        var request = new UpdateTenantCaseDefaultsRequest
        {
            DefaultTemplateKey = "template.v3"
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }
}
