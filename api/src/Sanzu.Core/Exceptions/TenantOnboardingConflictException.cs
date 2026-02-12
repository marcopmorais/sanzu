namespace Sanzu.Core.Exceptions;

public sealed class TenantOnboardingConflictException : Exception
{
    public TenantOnboardingConflictException(string message)
        : base(message)
    {
    }
}
