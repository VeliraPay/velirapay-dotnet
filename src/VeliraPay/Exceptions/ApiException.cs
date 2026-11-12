using System.Text.Json;

namespace VeliraPay;

/// <summary>The API answered with an error. Each kind of error has its own subclass, such as <see cref="NotFoundException"/>.</summary>
public class ApiException : VeliraPayException
{
    /// <summary>Create a new exception.</summary>
    /// <param name="message">The API's message, or a description of the status when it gave none.</param>
    /// <param name="response">The response that carried the error.</param>
    public ApiException(string message, ApiResponse response)
        : base(message)
    {
        ArgumentNullException.ThrowIfNull(response);

        Response = response;
        Status = response.Status;
        Body = response.Decode() ?? JsonValues.EmptyObject;
    }

    /// <summary>The HTTP status the API answered with.</summary>
    public int Status { get; }

    /// <summary>The decoded JSON body of the response: always a JSON object, empty when the body was not one.</summary>
    public JsonElement Body { get; }

    /// <summary>The response itself.</summary>
    public ApiResponse Response { get; }

    /// <summary>How long the API asked to wait before trying again, or null when it did not say.</summary>
    public TimeSpan? RetryAfter => Response.RetryAfter;

    /// <summary>The first value of a response header, or null when the response has none.</summary>
    /// <param name="name">The header's name, in any case.</param>
    public string? Header(string name) => Response.Header(name);

    /// <summary>Create the exception that matches the response's status.</summary>
    /// <param name="response">The unsuccessful response.</param>
    public static ApiException FromResponse(ApiResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var decoded = response.Decode();
        var message = decoded is { } body && body.TryGetProperty("message", out var value) && value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } text
            ? text
            : $"The VeliraPay API answered with HTTP {response.Status}.";

        return response.Status switch
        {
            401 => new AuthenticationException(message, response),
            403 => new PermissionException(message, response),
            404 => new NotFoundException(message, response),
            409 => new ConflictException(message, response),
            422 => new ValidationException(message, response),
            429 => new RateLimitException(message, response),
            >= 500 => new ServerException(message, response),
            _ => new InvalidRequestException(message, response),
        };
    }
}
