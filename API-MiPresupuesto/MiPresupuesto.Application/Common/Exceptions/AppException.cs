namespace MiPresupuesto.Application.Common.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(string message, IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message) => Errors = errors;

    public abstract int StatusCode { get; }
    public virtual string ErrorCode => "application_error";
    public IReadOnlyDictionary<string, string[]>? Errors { get; }
}

public sealed class ValidationException(
    string message,
    IReadOnlyDictionary<string, string[]>? errors = null) : AppException(message, errors)
{
    public override int StatusCode => 400;
    public override string ErrorCode => "validation_error";
}

public sealed class UnauthorizedException(string message) : AppException(message)
{
    public override int StatusCode => 401;
    public override string ErrorCode => "unauthorized";
}

public sealed class NotFoundException(string message) : AppException(message)
{
    public override int StatusCode => 404;
    public override string ErrorCode => "not_found";
}

public sealed class ConflictException(string message) : AppException(message)
{
    public override int StatusCode => 409;
    public override string ErrorCode => "conflict";
}
