namespace Sanzu.Core.Exceptions;

public sealed class CaseStateException : Exception
{
    public CaseStateException(string message)
        : base(message)
    {
    }
}
