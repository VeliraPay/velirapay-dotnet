using System.Text.Json;

namespace VeliraPay;

/// <summary>
/// An object the API answered with. Its typed properties read the fields this version of the library knows;
/// <see cref="RawJson"/> and <see cref="Get"/> keep every field the API sent, including ones added later.
/// </summary>
public abstract class ApiResource
{
    /// <summary>Create a resource from a JSON object.</summary>
    /// <param name="json">The object, as the API sent it.</param>
    /// <exception cref="InvalidArgumentException">The JSON is not an object.</exception>
    private protected ApiResource(JsonElement json)
    {
        if (json.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidArgumentException($"A {GetType().Name} is read from a JSON object, not from a JSON {json.ValueKind.ToString().ToLowerInvariant()}.");
        }

        RawJson = json.Clone();
    }

    /// <summary>The object as the API sent it.</summary>
    public JsonElement RawJson { get; }

    /// <summary>One field as the API sent it, including any this library has no property for yet; null when the object has no such field.</summary>
    /// <param name="key">The field's name, as the API gives it, such as "network_fee".</param>
    public JsonElement? Get(string key) => RawJson.TryGetProperty(key, out var value) ? value : null;

    /// <summary>The object as the API sent it, as JSON text. Mind that an API key or a webhook endpoint answered when made carries its secret.</summary>
    public string ToJson() => RawJson.GetRawText();
}
