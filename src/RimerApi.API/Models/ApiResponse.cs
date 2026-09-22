namespace RimerApi.API.Models;

/// <summary>
/// Consistent API response wrapper used by all controllers.
/// </summary>
/// <typeparam name="T">Type of data returned on success.</typeparam>
public class ApiResponse<T>
{
    /// <summary>Whether the request succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Response data (null on failure).</summary>
    public T? Data { get; set; }

    /// <summary>Error message (null on success).</summary>
    public string? Message { get; set; }

    /// <summary>HTTP status code.</summary>
    public int StatusCode { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message, StatusCode = 200 };

    public static ApiResponse<T> Created(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message, StatusCode = 201 };

    public static ApiResponse<T> Fail(string message, int statusCode = 400)
        => new() { Success = false, Message = message, StatusCode = statusCode };
}
