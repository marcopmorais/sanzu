namespace Sanzu.Core.Exceptions;

public sealed class TenantOnboardingStateException : Exception
{
    public TenantOnboardingStateException(string message)
        : base(message)
    {
    }
}
