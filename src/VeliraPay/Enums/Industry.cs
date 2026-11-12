namespace VeliraPay;

/// <summary>What an account sells, broadly.</summary>
public static class Industry
{
    /// <summary>Digital products.</summary>
    public const string DigitalProducts = "digital_products";

    /// <summary>Software.</summary>
    public const string Software = "software";

    /// <summary>Hosting.</summary>
    public const string Hosting = "hosting";

    /// <summary>Gaming.</summary>
    public const string Gaming = "gaming";

    /// <summary>Retail.</summary>
    public const string Retail = "retail";

    /// <summary>Professional services.</summary>
    public const string ProfessionalServices = "professional_services";

    /// <summary>Education.</summary>
    public const string Education = "education";

    /// <summary>Media.</summary>
    public const string Media = "media";

    /// <summary>A marketplace.</summary>
    public const string Marketplace = "marketplace";

    /// <summary>Travel.</summary>
    public const string Travel = "travel";

    /// <summary>Donations.</summary>
    public const string Donations = "donations";

    /// <summary>Anything else.</summary>
    public const string Other = "other";

    /// <summary>Every value this version of the library knows.</summary>
    internal static readonly string[] Values =
    [
        DigitalProducts, Software, Hosting, Gaming, Retail, ProfessionalServices, Education, Media, Marketplace, Travel, Donations, Other,
    ];
}
