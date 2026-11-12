using System.Globalization;
using System.Text.Json;

namespace VeliraPay;

/// <summary>A response from the VeliraPay API: its status, headers and raw body.</summary>
public sealed class ApiResponse
{
    private readonly Lazy<JsonElement?> decoded;

    /// <summary>Create a new response.</summary>
    /// <param name="status">The HTTP status.</param>
    /// <param name="headers">The header values, keyed by name in any case.</param>
    /// <param name="body">The raw body.</param>
    public ApiResponse(int status, IReadOnlyDictionary<string, IReadOnlyList<string>> headers, string body)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(body);

        Status = status;
        Headers = new Dictionary<string, IReadOnlyList<string>>(headers, StringComparer.OrdinalIgnoreCase);
        Body = body;
        decoded = new Lazy<JsonElement?>(Parse);
    }

    /// <summary>The HTTP status.</summary>
    public int Status { get; }

    /// <summary>The header values, keyed by name in any case.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; }

    /// <summary>The raw body.</summary>
    public string Body { get; }

    /// <summary>Whether the request succeeded, with a 2xx status.</summary>
    public bool IsSuccessful => Status is >= 200 and < 300;

    /// <summary>Whether the API replayed the response to an earlier request with the same idempotency key.</summary>
    public bool WasReplayed => string.Equals(Header("Idempotent-Replayed"), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>How long the API asked to wait before trying again, from a Retry-After header in seconds or as a date; null when it did not say.</summary>
    public TimeSpan? RetryAfter
    {
        get
        {
            var value = Header("Retry-After")?.Trim();

            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (value.All(char.IsAsciiDigit))
            {
                var seconds = long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ? Math.Min(parsed, int.MaxValue) : int.MaxValue;

                return TimeSpan.FromSeconds(seconds);
            }

            if (!DateTimeOffset.TryParseExact(value, "r", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
            {
                return null;
            }

            return TimeSpan.FromSeconds(Math.Max(0, Math.Ceiling((date - DateTimeOffset.UtcNow).TotalSeconds)));
        }
    }

    /// <summary>The first value of a header, or null when the response has none.</summary>
    /// <param name="name">The header's name, in any case.</param>
    public string? Header(string name) => Headers.TryGetValue(name, out var values) && values.Count > 0 ? values[0] : null;

    /// <summary>The body as a JSON object.</summary>
    /// <exception cref="UnexpectedResponseException">The body is not a JSON object.</exception>
    public JsonElement Json() => Decode() ?? throw new UnexpectedResponseException("The VeliraPay API answered with a body that is not a JSON object.", this);

    /// <summary>The body as a JSON object, or null when it is not one.</summary>
    public JsonElement? Decode() => decoded.Value;

    /// <summary>Parse the body, when it is a JSON object.</summary>
    private JsonElement? Parse()
    {
        if (string.IsNullOrWhiteSpace(Body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(Body);

            return document.RootElement.ValueKind == JsonValueKind.Object ? document.RootElement.Clone() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
