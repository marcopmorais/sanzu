namespace Sanzu.Core.Models.Requests;

public sealed class CreateAgencyAccountRequest
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AgencyName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}
