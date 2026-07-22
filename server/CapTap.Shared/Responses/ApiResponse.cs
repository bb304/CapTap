namespace CapTap.Shared.Responses;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }

    public T? Data { get; init; }

    public string? Message { get; init; }

    public ApiError? Error { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new()
        {
            Success = true,
            Data = data,
            Message = message
        };

    public static ApiResponse<T> Fail(string code, string message) =>
        new()
        {
            Success = false,
            Error = new ApiError
            {
                Code = code,
                Message = message
            }
        };
}

public sealed class ApiResponse
{
    public bool Success { get; init; }

    public object? Data { get; init; }

    public string? Message { get; init; }

    public ApiError? Error { get; init; }

    public static ApiResponse Ok(object? data = null, string? message = null) =>
        new()
        {
            Success = true,
            Data = data,
            Message = message
        };

    public static ApiResponse Fail(string code, string message) =>
        new()
        {
            Success = false,
            Error = new ApiError
            {
                Code = code,
                Message = message
            }
        };
}

public sealed class ApiError
{
    public required string Code { get; init; }

    public required string Message { get; init; }
}
