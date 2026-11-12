namespace VeliraPay;

/// <summary>
/// The parameters of a request. Only the properties you set are sent, under the names the API gives them, in the order you set them;
/// one set to null is sent as null, which clears that field. Amounts are decimals, sent as strings so no precision is lost,
/// times are sent in UTC whatever their offset, and days as YYYY-MM-DD.
/// </summary>
public abstract class BaseOptions
{
    private readonly List<KeyValuePair<string, object?>> parameters = [];

    /// <summary>Create a new set of parameters.</summary>
    private protected BaseOptions()
    {
    }

    /// <summary>The parameters set so far, in the order they were set.</summary>
    internal IReadOnlyList<KeyValuePair<string, object?>> Parameters => parameters;

    /// <summary>Send a parameter this version of the library has no property for, under the name the API gives it.</summary>
    /// <param name="name">The parameter's name, such as "description".</param>
    /// <param name="value">
    /// A string, number, decimal, boolean, <see cref="DateTimeOffset"/>, <see cref="DateOnly"/>, list, dictionary, options object or JSON value, or null.
    /// A decimal goes as a string, as amounts should; a double or a float goes as a JSON number, written without an exponent.
    /// </param>
    public void AddExtraParam(string name, object? value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidArgumentException("A parameter sent to the VeliraPay API needs a name.");
        }

        Set(name, value);
    }

    /// <summary>Read a parameter that was set, or the default when it was not.</summary>
    private protected T? Get<T>(string name)
    {
        foreach (var (key, value) in parameters)
        {
            if (key == name)
            {
                return value is T typed ? typed : default;
            }
        }

        return default;
    }

    /// <summary>Set a parameter, keeping its place when it was set before.</summary>
    private protected void Set(string name, object? value)
    {
        for (var index = 0; index < parameters.Count; index++)
        {
            if (parameters[index].Key == name)
            {
                parameters[index] = new KeyValuePair<string, object?>(name, value);

                return;
            }
        }

        parameters.Add(new KeyValuePair<string, object?>(name, value));
    }
}

/// <summary>Which page of a list to fetch.</summary>
public class ListOptions : BaseOptions
{
    /// <summary>The page, from 1.</summary>
    public int? Page { get => Get<int?>("page"); set => Set("page", value); }

    /// <summary>How many objects a page holds, up to 100; 25 when left out.</summary>
    public int? PerPage { get => Get<int?>("per_page"); set => Set("per_page", value); }
}
