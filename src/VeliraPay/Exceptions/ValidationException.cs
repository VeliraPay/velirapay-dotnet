using System.Text.Json;

namespace VeliraPay;

/// <summary>The request failed validation (422). <see cref="Errors"/> gives the messages by field.</summary>
public sealed class ValidationException : ApiException
{
    /// <inheritdoc cref="ApiException(string, ApiResponse)"/>
    public ValidationException(string message, ApiResponse response)
        : base(message, response)
    {
        Errors = ReadErrors(Body);
    }

    /// <summary>The validation messages keyed by field, such as "amount" or "items.0.quantity".</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; }

    /// <summary>The first validation message for a field, or null when it has none.</summary>
    /// <param name="field">The field, such as "amount".</param>
    public string? FirstError(string field) => Errors.TryGetValue(field, out var messages) && messages.Count > 0 ? messages[0] : null;

    /// <summary>Read the "errors" of a validation answer.</summary>
    private static Dictionary<string, IReadOnlyList<string>> ReadErrors(JsonElement body)
    {
        var errors = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

        foreach (var (field, messages) in JsonValues.EnumerateMap(body, "errors"))
        {
            errors[field] = messages.ValueKind switch
            {
                JsonValueKind.Array => [.. messages.EnumerateArray().Where(message => message.ValueKind == JsonValueKind.String).Select(message => message.GetString()!)],
                JsonValueKind.String => [messages.GetString()!],
                _ => [],
            };
        }

        return errors;
    }
}
