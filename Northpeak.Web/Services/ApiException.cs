namespace Northpeak.Web.Services;

public sealed class ApiException(string message, Exception? innerException = null)
    : Exception(message, innerException);
