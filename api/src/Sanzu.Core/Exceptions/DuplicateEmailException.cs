namespace Sanzu.Core.Exceptions;

public sealed class DuplicateEmailException : Exception
{
    public DuplicateEmailException()
        : base("Unable to create account with the provided information.")
    {
    }
}
