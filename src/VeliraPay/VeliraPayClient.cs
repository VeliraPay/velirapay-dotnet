using System.Net;

namespace VeliraPay;

/// <summary>
/// A client for the VeliraPay API, authenticated with one API key. It is safe to share between threads; keep one for the life of your application,
/// or let <c>IHttpClientFactory</c> make one per scope as a typed client.
/// </summary>
public sealed class VeliraPayClient
{
    /// <summary>The version of this library.</summary>
    public const string Version = "0.1.0";

    /// <summary>The address of the VeliraPay API.</summary>
    public const string DefaultBaseUrl = "https://api.velirapay.com";

    /// <summary>The HTTP client the library uses when none is given, shared by every VeliraPayClient so connections are reused.</summary>
    private static readonly Lazy<HttpClient> SharedHttpClient = new(() => new HttpClient(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.All,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
    })
    {
        Timeout = System.Threading.Timeout.InfiniteTimeSpan,
    });

    private readonly HttpTransport transport;

    /// <summary>Create a client that sends its requests through an HTTP client the library shares between its clients.</summary>
    /// <param name="apiKey">A secret key from the dashboard, starting with "vp_live_" or "vp_test_". Keep it on your server.</param>
    /// <param name="options">How to reach the API; the defaults when null.</param>
    /// <exception cref="InvalidArgumentException">The key is missing or malformed, or an option is out of range.</exception>
    public VeliraPayClient(string? apiKey, VeliraPayClientOptions? options = null)
        : this(apiKey, SharedHttpClient.Value, options, null)
    {
    }

    /// <summary>
    /// Create a client that sends its requests through your own HTTP client, such as a typed client from <c>IHttpClientFactory</c>.
    /// The library does not dispose it. Its handler should not follow redirects, which the API never answers with but a proxy might.
    /// </summary>
    /// <param name="apiKey">A secret key from the dashboard, starting with "vp_live_" or "vp_test_". Keep it on your server.</param>
    /// <param name="httpClient">The HTTP client to send requests with. Its own <see cref="HttpClient.Timeout"/> applies as well as <see cref="VeliraPayClientOptions.Timeout"/>.</param>
    /// <param name="options">How to reach the API; the defaults when null.</param>
    /// <exception cref="InvalidArgumentException">The key is missing or malformed, or an option is out of range.</exception>
    public VeliraPayClient(string? apiKey, HttpClient httpClient, VeliraPayClientOptions? options = null)
        : this(apiKey, httpClient ?? throw new ArgumentNullException(nameof(httpClient)), options, null)
    {
    }

    /// <summary>Create a client whose waits between attempts can be watched, for tests.</summary>
    internal VeliraPayClient(string? apiKey, HttpClient httpClient, VeliraPayClientOptions? options, Func<TimeSpan, CancellationToken, Task>? delay)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidArgumentException("A VeliraPay API key is required. Create one in the dashboard under Developers > API keys.");
        }

        Mode = apiKey.StartsWith("vp_live_", StringComparison.Ordinal) ? VeliraPay.Mode.Live
            : apiKey.StartsWith("vp_test_", StringComparison.Ordinal) ? VeliraPay.Mode.Test
            : throw new InvalidArgumentException("A VeliraPay API key starts with \"vp_live_\" or \"vp_test_\". Copy yours from the dashboard under Developers > API keys.");

        if (!ParamsEncoder.IsHeaderSafe(apiKey, allowSpaces: false))
        {
            throw new InvalidArgumentException("The API key holds a character no key has, such as a space or a line break. Check how it is loaded.");
        }

        options ??= new VeliraPayClientOptions();

        if (options.MaxRetries < 0)
        {
            throw new InvalidArgumentException("The number of retries cannot be negative.");
        }

        if (options.MaxRetryAfter < TimeSpan.Zero)
        {
            throw new InvalidArgumentException("The longest Retry-After to wait out cannot be negative.");
        }

        if (options.Timeout != System.Threading.Timeout.InfiniteTimeSpan && (options.Timeout <= TimeSpan.Zero || options.Timeout.TotalMilliseconds > int.MaxValue))
        {
            throw new InvalidArgumentException("The timeout must be a positive duration of at most 24 days, or Timeout.InfiniteTimeSpan.");
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUrl) || (baseUrl.Scheme != Uri.UriSchemeHttps && baseUrl.Scheme != Uri.UriSchemeHttp) || baseUrl.Host.Length == 0)
        {
            throw new InvalidArgumentException("The base URL must be an http or https address.");
        }

        var userAgent = $"VeliraPay-DotNet/{Version} .NET/{Environment.Version}";

        if (!string.IsNullOrWhiteSpace(options.AppInfo))
        {
            if (!ParamsEncoder.IsHeaderSafe(options.AppInfo, allowSpaces: true))
            {
                throw new InvalidArgumentException("The app info can only hold printable ASCII characters, as it is sent in the User-Agent header.");
            }

            userAgent += $" {options.AppInfo.Trim()}";
        }

        transport = new HttpTransport(httpClient, apiKey, options.BaseUrl, options.MaxRetries, options.MaxRetryAfter, options.Timeout, userAgent, delay);

        Account = new AccountService(transport);
        Assets = new AssetService(transport);
        Charges = new ChargeService(transport);
        PaymentLinks = new PaymentLinkService(transport);
        Invoices = new InvoiceService(transport);
        Coupons = new CouponService(transport);
        Payouts = new PayoutService(transport);
        Events = new EventService(transport);
        WebhookEndpoints = new WebhookEndpointService(transport);
        Overview = new OverviewService(transport);
        PayoutAddresses = new PayoutAddressService(transport);
        Activity = new ActivityService(transport);
        ApiKeys = new ApiKeyService(transport);
        Members = new MemberService(transport);
        Invitations = new InvitationService(transport);
        Billing = new BillingService(transport);
    }

    /// <summary>The account the API key belongs to.</summary>
    public AccountService Account { get; }

    /// <summary>Assets: the coins on offer, and the lowest price each can be charged at.</summary>
    public AssetService Assets { get; }

    /// <summary>Charges: how to ask a customer for a payment, in one coin.</summary>
    public ChargeService Charges { get; }

    /// <summary>Payment links: reusable checkout pages many customers pay through.</summary>
    public PaymentLinkService PaymentLinks { get; }

    /// <summary>Invoices: itemised bills customers pay in the coin of their choice.</summary>
    public InvoiceService Invoices { get; }

    /// <summary>Coupons: discount codes for the hosted checkout and new charges.</summary>
    public CouponService Coupons { get; }

    /// <summary>Payouts: what became of each paid charge.</summary>
    public PayoutService Payouts { get; }

    /// <summary>Events: the history of every charge and invoice.</summary>
    public EventService Events { get; }

    /// <summary>Webhook endpoints: where webhooks are delivered, and whether they can be.</summary>
    public WebhookEndpointService WebhookEndpoints { get; }

    /// <summary>The overview: the dashboard's figures for the last 30 days.</summary>
    public OverviewService Overview { get; }

    /// <summary>Payout addresses: where the account's payouts go on each network.</summary>
    public PayoutAddressService PayoutAddresses { get; }

    /// <summary>The activity log: everything done in the account, and who did it.</summary>
    public ActivityService Activity { get; }

    /// <summary>API keys: the account's keys, never reaching further than the one asking.</summary>
    public ApiKeyService ApiKeys { get; }

    /// <summary>Members: the people in the account and their roles.</summary>
    public MemberService Members { get; }

    /// <summary>Invitations: who has been asked to join and has not yet.</summary>
    public InvitationService Invitations { get; }

    /// <summary>Billing: paying for Pro in crypto.</summary>
    public BillingService Billing { get; }

    /// <summary>The mode the API key works in, read from its prefix: <see cref="VeliraPay.Mode.Live"/> or <see cref="VeliraPay.Mode.Test"/>.</summary>
    public string Mode { get; }

    /// <summary>Whether the API key moves real funds.</summary>
    public bool IsLiveMode => Mode == VeliraPay.Mode.Live;

    /// <summary>Whether the API key works on test networks.</summary>
    public bool IsTestMode => Mode == VeliraPay.Mode.Test;

    /// <summary>
    /// The response to the most recent request this client made, such as to read its headers, or null before the first one.
    /// With requests running side by side, it is whichever finished last.
    /// </summary>
    public ApiResponse? LastResponse => transport.LastResponse;

    /// <summary>Send a request to an endpoint this library has no method for yet.</summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The path, such as "/v1/charges".</param>
    /// <param name="parameters">The query string of a GET request, and the JSON body of any other; null values are left out of a query string. Decimals are sent as strings.</param>
    /// <param name="requestOptions">The idempotency key of a request that changes something.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The successful response; <see cref="ApiResponse.Json"/> decodes it.</returns>
    public Task<ApiResponse> RequestAsync(HttpMethod method, string path, IReadOnlyDictionary<string, object?>? parameters = null, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(path);

        if (string.Equals(method.Method, HttpMethod.Get.Method, StringComparison.OrdinalIgnoreCase))
        {
            return transport.RequestAsync(HttpMethod.Get, path, parameters, null, null, cancellationToken);
        }

        return transport.RequestAsync(method, path, null, parameters is null || parameters.Count == 0 ? null : parameters, requestOptions?.IdempotencyKey, cancellationToken);
    }
}
