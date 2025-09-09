namespace CertVal.Application.Common.Exceptions;

public class ForbiddenException : ApplicationException
{
    public ForbiddenException(string message = "Forbidden access") : base(message) { }
}