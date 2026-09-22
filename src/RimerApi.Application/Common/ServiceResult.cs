namespace RimerApi.Application.Common;

/// <summary>
/// Standard service result wrapper providing success/failure semantics with data and error messages.
/// </summary>
/// <typeparam name="T">The type of data returned on success.</typeparam>
public class ServiceResult<T>
{
    /// <summary>Whether the operation succeeded.</summary>
    public bool IsSuccess { get; private set; }

    /// <summary>The result data (null on failure).</summary>
    public T? Data { get; private set; }

    /// <summary>Error message explaining the failure (null on success).</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>HTTP-friendly error code for the API layer to use.</summary>
    public int? ErrorCode { get; private set; }

    /// <summary>Create a successful result with data.</summary>
    public static ServiceResult<T> Success(T data)
        => new() { IsSuccess = true, Data = data };

    /// <summary>Create a failure result with an error message and optional error code.</summary>
    public static ServiceResult<T> Failure(string errorMessage, int errorCode = 400)
        => new() { IsSuccess = false, ErrorMessage = errorMessage, ErrorCode = errorCode };

    /// <summary>Create a "not found" failure result.</summary>
    public static ServiceResult<T> NotFound(string message = "Resource not found.")
        => new() { IsSuccess = false, ErrorMessage = message, ErrorCode = 404 };

    /// <summary>Create a "forbidden" failure result (authenticated but not authorized).</summary>
    public static ServiceResult<T> Forbidden(string message = "You do not have permission to perform this action.")
        => new() { IsSuccess = false, ErrorMessage = message, ErrorCode = 403 };
}
