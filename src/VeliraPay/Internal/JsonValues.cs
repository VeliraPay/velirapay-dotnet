using System.Globalization;
using System.Text.Json;

namespace VeliraPay;

/// <summary>Reads the attributes of the JSON objects the API answers with, leniently: a value of another type reads as missing.</summary>
internal static class JsonValues
{
    /// <summary>An empty JSON object.</summary>
    public static readonly JsonElement EmptyObject = Parse("{}");

    /// <summary>A JSON value that is no longer tied to the document it was parsed from.</summary>
    public static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);

        return document.RootElement.Clone();
    }

    /// <summary>Find an attribute of an object.</summary>
    public static bool TryGet(JsonElement data, string key, out JsonElement value)
    {
        if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty(key, out value))
        {
            return true;
        }

        value = default;

        return false;
    }

    /// <summary>Read a string attribute, or the given fallback.</summary>
    public static string String(JsonElement data, string key, string fallback = "") => NullableString(data, key) ?? fallback;

    /// <summary>Read a string attribute that may be null; a number reads as its digits.</summary>
    public static string? NullableString(JsonElement data, string key)
    {
        if (!TryGet(data, key, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null,
        };
    }

    /// <summary>Read a boolean attribute, or the given fallback.</summary>
    public static bool Bool(JsonElement data, string key, bool fallback = false) => NullableBool(data, key) ?? fallback;

    /// <summary>Read a boolean attribute that may be null.</summary>
    public static bool? NullableBool(JsonElement data, string key)
    {
        if (!TryGet(data, key, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    /// <summary>Read an integer attribute, or the given fallback.</summary>
    public static int Int(JsonElement data, string key, int fallback = 0) => NullableInt(data, key) ?? fallback;

    /// <summary>Read an integer attribute that may be null; a string of digits reads as its number.</summary>
    public static int? NullableInt(JsonElement data, string key) => TryGet(data, key, out var value) ? Integer(value) : null;

    /// <summary>Read an integer value, given as a number or as a string of digits.</summary>
    public static int? Integer(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number)
        {
            return value.TryGetInt32(out var number) ? number : null;
        }

        if (value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } text && text.All(char.IsAsciiDigit))
        {
            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
        }

        return null;
    }

    /// <summary>Read an amount, or the given fallback.</summary>
    public static decimal Decimal(JsonElement data, string key, decimal fallback = 0m) => NullableDecimal(data, key) ?? fallback;

    /// <summary>Read an amount that may be null, given as a decimal string, as the API sends amounts, or as a number.</summary>
    public static decimal? NullableDecimal(JsonElement data, string key) => TryGet(data, key, out var value) ? Amount(value) : null;

    /// <summary>Read an amount, keeping every decimal place it was written with.</summary>
    public static decimal? Amount(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number)
        {
            return value.TryGetDecimal(out var number) ? number : null;
        }

        if (value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } text)
        {
            return decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
        }

        return null;
    }

    /// <summary>Read a list of amounts, leaving out what is not one.</summary>
    public static IReadOnlyList<decimal> Amounts(JsonElement data, string key)
    {
        if (!TryGet(data, key, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return [.. value.EnumerateArray().Select(Amount).OfType<decimal>()];
    }

    /// <summary>Read a timestamp, or a date, which is taken as midnight UTC.</summary>
    public static DateTimeOffset? Date(JsonElement data, string key)
    {
        if (!TryGet(data, key, out var value) || value.ValueKind != JsonValueKind.String || value.GetString() is not { Length: > 0 } text)
        {
            return null;
        }

        if (IsDay(text))
        {
            return DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
                ? new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
                : null;
        }

        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : null;
    }

    /// <summary>Read a day, given as a date or as a timestamp, whose own day it is.</summary>
    public static DateOnly? Day(JsonElement data, string key)
    {
        if (!TryGet(data, key, out var value) || value.ValueKind != JsonValueKind.String || value.GetString() is not { Length: > 0 } text)
        {
            return null;
        }

        if (IsDay(text))
        {
            return DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day) ? day : null;
        }

        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? DateOnly.FromDateTime(parsed.DateTime) : null;
    }

    /// <summary>Read a list of strings.</summary>
    public static IReadOnlyList<string> Strings(JsonElement data, string key) => NullableStrings(data, key) ?? [];

    /// <summary>Read a list of strings that may be null; numbers in it read as their digits, and anything else is left out.</summary>
    public static IReadOnlyList<string>? NullableStrings(JsonElement data, string key)
    {
        if (!TryGet(data, key, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        return
        [
            .. value.EnumerateArray()
                .Where(item => item.ValueKind is JsonValueKind.String or JsonValueKind.Number)
                .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString()! : item.GetRawText()),
        ];
    }

    /// <summary>The entries of an object attribute, in order; an empty map arrives from the API as an empty array, and a list is keyed by position.</summary>
    public static IEnumerable<KeyValuePair<string, JsonElement>> EnumerateMap(JsonElement data, string key)
    {
        if (!TryGet(data, key, out var value))
        {
            return [];
        }

        return value.ValueKind switch
        {
            JsonValueKind.Object => value.EnumerateObject().Select(property => new KeyValuePair<string, JsonElement>(property.Name, property.Value)),
            JsonValueKind.Array => value.EnumerateArray().Select((item, index) => new KeyValuePair<string, JsonElement>(index.ToString(CultureInfo.InvariantCulture), item)),
            _ => [],
        };
    }

    /// <summary>Read an object attribute as a map of its entries.</summary>
    public static IReadOnlyDictionary<string, JsonElement> Map(JsonElement data, string key)
    {
        var map = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        foreach (var (name, value) in EnumerateMap(data, key))
        {
            map[name] = value;
        }

        return map;
    }

    /// <summary>Read an object attribute as a JSON object of its own, empty when it is missing.</summary>
    public static JsonElement Object(JsonElement data, string key)
    {
        if (TryGet(data, key, out var value) && value.ValueKind == JsonValueKind.Object)
        {
            return value;
        }

        return Write(writer =>
        {
            writer.WriteStartObject();

            foreach (var (name, item) in EnumerateMap(data, key))
            {
                writer.WritePropertyName(name);
                item.WriteTo(writer);
            }

            writer.WriteEndObject();
        });
    }

    /// <summary>Read the objects of a list attribute, leaving out what is not an object.</summary>
    public static IEnumerable<JsonElement> Records(JsonElement data, string key)
    {
        if (!TryGet(data, key, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return value.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.Object);
    }

    /// <summary>Read the objects of a list attribute into resources.</summary>
    public static IReadOnlyList<T> Objects<T>(JsonElement data, string key, Func<JsonElement, T> hydrate) => [.. Records(data, key).Select(hydrate)];

    /// <summary>Read an object attribute into a resource, or null when it is not an object.</summary>
    public static T? NullableObject<T>(JsonElement data, string key, Func<JsonElement, T> hydrate)
        where T : class
        => TryGet(data, key, out var value) && value.ValueKind == JsonValueKind.Object ? hydrate(value) : null;

    /// <summary>Read the code of a related object, sent either as the code itself or as an object carrying it.</summary>
    public static string? Reference(JsonElement data, string key)
    {
        if (!TryGet(data, key, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            if (!value.TryGetProperty("id", out var id) || id.ValueKind == JsonValueKind.Null)
            {
                value.TryGetProperty("code", out id);
            }

            value = id;
        }

        return value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } code ? code : null;
    }

    /// <summary>Read a string attribute only when it is one of the given values.</summary>
    public static string? OneOf(JsonElement data, string key, string[] values)
        => NullableString(data, key) is { } value && Array.IndexOf(values, value) >= 0 ? value : null;

    /// <summary>Write a JSON value and read it back.</summary>
    public static JsonElement Write(Action<Utf8JsonWriter> write)
    {
        var buffer = new System.Buffers.ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            write(writer);
        }

        using var document = JsonDocument.Parse(buffer.WrittenMemory);

        return document.RootElement.Clone();
    }

    /// <summary>Whether a value is a date written as YYYY-MM-DD.</summary>
    private static bool IsDay(string text)
        => text.Length == 10 && text[4] == '-' && text[7] == '-' && text.Where((character, index) => index is not (4 or 7)).All(char.IsAsciiDigit);
}
