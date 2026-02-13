namespace Sanzu.Core.Exceptions;

public sealed class SupportDiagnosticAccessException : Exception
{
    public SupportDiagnosticAccessException(string message)
        : base(message)
    {
    }
}
