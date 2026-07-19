using Microsoft.AspNetCore.Http;

namespace eCommerce.Server.Application.Common.Results;

public class Result
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public string? Error { get; init; }

    public static Result Ok(int statusCode = StatusCodes.Status200OK) => new()
    {
        Success = true,
        StatusCode = statusCode
    };

    public static Result Fail(int statusCode, string error) => new()
    {
        Success = false,
        StatusCode = statusCode,
        Error = error
    };
}

public class Result<T> : Result
{
    public T? Data { get; init; }

    public static Result<T> Ok(T data, int statusCode = StatusCodes.Status200OK) => new()
    {
        Success = true,
        StatusCode = statusCode,
        Data = data
    };

    public new static Result<T> Fail(int statusCode, string error) => new()
    {
        Success = false,
        StatusCode = statusCode,
        Error = error
    };
}
