namespace VeliraPay;

/// <summary>The mode an API key, object or webhook belongs to.</summary>
public static class Mode
{
    /// <summary>Real money on the real chains.</summary>
    public const string Live = "live";

    /// <summary>Worthless coins on the test networks.</summary>
    public const string Test = "test";
}
