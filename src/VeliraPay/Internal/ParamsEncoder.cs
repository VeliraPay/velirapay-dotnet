using System.Buffers;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace VeliraPay;

/// <summary>Turns request parameters into the JSON body or the query string the API reads.</summary>
internal static class ParamsEncoder
{
    /// <summary>How deeply parameters may nest, which also stops a dictionary that holds itself.</summary>
    private const int MaxDepth = 64;

    /// <summary>JSON as PHP's json_encode() writes it with JSON_UNESCAPED_UNICODE and JSON_UNESCAPED_SLASHES.</summary>
    private static readonly JsonWriterOptions WriterOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>Encode parameters as a JSON object.</summary>
    public static byte[] Json(IEnumerable<KeyValuePair<string, object?>> parameters)
    {
        var buffer = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer, WriterOptions))
        {
            WriteObject(writer, parameters, 0);
        }

        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>Encode parameters as a query string, leaving out nulls, as PHP's http_build_query() does.</summary>
    public static string Query(IEnumerable<KeyValuePair<string, object?>> parameters)
    {
        var pairs = new List<string>();

        foreach (var (name, value) in parameters)
        {
            AppendQuery(pairs, name, value, 0);
        }

        return string.Join('&', pairs);
    }

    /// <summary>A timestamp as the API reads it, such as "2026-12-31T23:59:59+00:00".</summary>
    public static string Timestamp(DateTimeOffset value) => value.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'sszzz", CultureInfo.InvariantCulture);

    /// <summary>A day as the API reads it, such as "2026-10-15".</summary>
    public static string Day(DateOnly value) => value.ToString("yyyy'-'MM'-'dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// An amount as a decimal string without trailing zeros, such as "59.88" for 59.8800m: the API counts every decimal place written,
    /// and decimal arithmetic keeps the places of both operands. Never in exponent notation, and never rounded.
    /// </summary>
    public static string Amount(decimal value) => value.ToString("0.############################", CultureInfo.InvariantCulture);

    /// <summary>Write the entries of an object.</summary>
    private static void WriteObject(Utf8JsonWriter writer, IEnumerable<KeyValuePair<string, object?>> entries, int depth)
    {
        writer.WriteStartObject();

        foreach (var (name, value) in entries)
        {
            writer.WritePropertyName(name);
            WriteValue(writer, value, depth + 1);
        }

        writer.WriteEndObject();
    }

    /// <summary>Write one value.</summary>
    private static void WriteValue(Utf8JsonWriter writer, object? value, int depth)
    {
        if (depth > MaxDepth)
        {
            throw TooDeep();
        }

        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string text:
                writer.WriteStringValue(text);
                break;
            case bool flag:
                writer.WriteBooleanValue(flag);
                break;
            case decimal amount:
                writer.WriteStringValue(Amount(amount));
                break;
            case sbyte or byte or short or ushort or int or long:
                writer.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                break;
            case uint or ulong:
                writer.WriteNumberValue(Convert.ToUInt64(value, CultureInfo.InvariantCulture));
                break;
            case double or float:
                writer.WriteNumberValue(Finite(Convert.ToDouble(value, CultureInfo.InvariantCulture)));
                break;
            case DateTimeOffset timestamp:
                writer.WriteStringValue(Timestamp(timestamp));
                break;
            case DateOnly day:
                writer.WriteStringValue(Day(day));
                break;
            case DateTime:
                throw new InvalidArgumentException("A DateTime cannot be sent to the VeliraPay API, as it may not say which time zone it is in. Pass a DateTimeOffset, or a DateOnly for a day.");
            case JsonElement element:
                element.WriteTo(writer);
                break;
            case JsonNode node:
                node.WriteTo(writer);
                break;
            case BaseOptions options:
                WriteObject(writer, options.Parameters, depth);
                break;
            case IEnumerable<KeyValuePair<string, object?>> entries:
                WriteObject(writer, entries, depth);
                break;
            case IDictionary dictionary:
                WriteObject(writer, Entries(dictionary), depth);
                break;
            case IEnumerable items:
                writer.WriteStartArray();

                foreach (var item in items)
                {
                    WriteValue(writer, item, depth + 1);
                }

                writer.WriteEndArray();
                break;
            default:
                throw Unsupported(value);
        }
    }

    /// <summary>Add one parameter to a query string, its entries as name[key] and its items as name[0], name[1].</summary>
    private static void AppendQuery(List<string> pairs, string name, object? value, int depth)
    {
        if (depth > MaxDepth)
        {
            throw TooDeep();
        }

        switch (value)
        {
            case null:
                return;
            case string text:
                pairs.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(text)}");
                return;
            case bool flag:
                pairs.Add($"{Uri.EscapeDataString(name)}={(flag ? "1" : "0")}");
                return;
            case decimal amount:
                pairs.Add($"{Uri.EscapeDataString(name)}={Amount(amount)}");
                return;
            case int or long or short or sbyte or byte or ushort or uint or ulong:
                pairs.Add($"{Uri.EscapeDataString(name)}={Convert.ToString(value, CultureInfo.InvariantCulture)}");
                return;
            case double or float:
                pairs.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(Finite(Convert.ToDouble(value, CultureInfo.InvariantCulture)).ToString("R", CultureInfo.InvariantCulture))}");
                return;
            case DateTimeOffset timestamp:
                pairs.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(Timestamp(timestamp))}");
                return;
            case DateOnly day:
                pairs.Add($"{Uri.EscapeDataString(name)}={Day(day)}");
                return;
            case DateTime:
                throw new InvalidArgumentException("A DateTime cannot be sent to the VeliraPay API, as it may not say which time zone it is in. Pass a DateTimeOffset, or a DateOnly for a day.");
            case JsonElement element:
                AppendQuery(pairs, name, Plain(element), depth + 1);
                return;
            case BaseOptions options:
                AppendEntries(pairs, name, options.Parameters, depth);
                return;
            case IEnumerable<KeyValuePair<string, object?>> entries:
                AppendEntries(pairs, name, entries, depth);
                return;
            case IDictionary dictionary:
                AppendEntries(pairs, name, Entries(dictionary), depth);
                return;
            case IEnumerable items:
                var index = 0;

                foreach (var item in items)
                {
                    AppendQuery(pairs, $"{name}[{index++}]", item, depth + 1);
                }

                return;
            default:
                throw Unsupported(value);
        }
    }

    /// <summary>Add the entries of an object to a query string.</summary>
    private static void AppendEntries(List<string> pairs, string name, IEnumerable<KeyValuePair<string, object?>> entries, int depth)
    {
        foreach (var (key, item) in entries)
        {
            AppendQuery(pairs, $"{name}[{key}]", item, depth + 1);
        }
    }

    /// <summary>A JSON value as the plain value a query string can carry.</summary>
    private static object? Plain(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetDecimal(out var number) ? number : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Object => element.EnumerateObject().Select(property => new KeyValuePair<string, object?>(property.Name, property.Value)).ToList(),
        JsonValueKind.Array => element.EnumerateArray().Select(item => (object?)item).ToList(),
        _ => null,
    };

    /// <summary>The entries of a dictionary, whose keys must be strings.</summary>
    private static IEnumerable<KeyValuePair<string, object?>> Entries(IDictionary dictionary)
    {
        foreach (DictionaryEntry entry in dictionary)
        {
            if (entry.Key is not string key)
            {
                throw new InvalidArgumentException($"The parameters sent to the VeliraPay API need string keys, not {entry.Key.GetType().Name} ones.");
            }

            yield return new KeyValuePair<string, object?>(key, entry.Value);
        }
    }

    /// <summary>A number JSON can carry.</summary>
    private static double Finite(double value)
        => double.IsFinite(value) ? value : throw new InvalidArgumentException("The request could not be encoded as JSON: NaN and infinity are not numbers JSON can carry.");

    /// <summary>The exception for parameters that nest too deeply.</summary>
    private static InvalidArgumentException TooDeep()
        => new($"The request could not be encoded: its parameters nest more than {MaxDepth} levels deep, or a dictionary or list holds itself.");

    /// <summary>The exception for a value the API cannot be sent.</summary>
    private static InvalidArgumentException Unsupported(object value)
        => new($"A {value.GetType().Name} cannot be sent to the VeliraPay API. Pass a string, a number, a decimal, a boolean, a DateTimeOffset, a DateOnly, a list or a dictionary.");

    /// <summary>Whether a header value is safe to send: printable ASCII, so it cannot add a header of its own.</summary>
    public static bool IsHeaderSafe(string value, bool allowSpaces)
        => value.All(character => character is > ' ' and <= '~' || (allowSpaces && character == ' '));

    /// <summary>Encode text as UTF-8.</summary>
    public static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);
}
