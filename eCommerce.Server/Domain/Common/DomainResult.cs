namespace eCommerce.Server.Domain.Common;

public enum DomainErrorCode
{
    None = 0,
    Validation = 1,
    NotFound = 2
}

public sealed record DomainResult(bool IsSuccess, string? Error = null, DomainErrorCode ErrorCode = DomainErrorCode.None)
{
    public static DomainResult Success() => new(true);
    public static DomainResult Validation(string error) => new(false, error, DomainErrorCode.Validation);
    public static DomainResult NotFound(string error) => new(false, error, DomainErrorCode.NotFound);
}
