namespace Sanzu.Core.Models.Responses;

public sealed class GlossaryLookupResponse
{
    public IReadOnlyList<GlossaryTermResponse> Terms { get; init; } = Array.Empty<GlossaryTermResponse>();
}

