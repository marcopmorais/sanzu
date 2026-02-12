namespace Sanzu.Core.Exceptions;

public sealed class TenantAccessDeniedException : Exception
{
    public TenantAccessDeniedException()
        : base("Tenant access denied.")
    {
    }
}
