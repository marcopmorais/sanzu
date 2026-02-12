namespace Sanzu.Core.Models.Responses;

public sealed class ApiEnvelope<T>
{
    public T? Data { get; init; }
    public IReadOnlyCollection<ApiError> Errors { get; init; } = Array.Empty<ApiError>();
    public object? Meta { get; init; }

    public static ApiEnvelope<T> Success(T data, object? meta = null)
    {
        return new ApiEnvelope<T>
        {
            Data = data,
            Errors = Array.Empty<ApiError>(),
            Meta = meta
        };
    }

    public static ApiEnvelope<T> Failure(IEnumerable<ApiError> errors, object? meta = null)
    {
        return new ApiEnvelope<T>
        {
            Data = default,
            Errors = errors.ToArray(),
            Meta = meta
        };
    }
}
