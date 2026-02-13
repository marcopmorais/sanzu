namespace Sanzu.Core.Exceptions;

public sealed class TenantLifecycleStateException : Exception
{
    public TenantLifecycleStateException(string message)
        : base(message)
    {
    }
}
