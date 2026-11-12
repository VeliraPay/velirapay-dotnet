using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace VeliraPay;

/// <summary>Verifies and parses the webhooks VeliraPay sends.</summary>
public static class Webhook
{
    /// <summary>The header carrying the delivery's signature: <c>t={unix time},v1={signature}</c>.</summary>
    public const string SignatureHeader = "X-VeliraPay-Signature";

    /// <summary>The header carrying the event type.</summary>
    public const string EventHeader = "X-VeliraPay-Event";

    /// <summary>The header carrying the delivery's id.</summary>
    public const string DeliveryHeader = "X-VeliraPay-Delivery";

    /// <summary>The event of the challenge sent to an endpoint when it is added or moved: it receives nothing else until it answers.</summary>
    public const string VerificationEvent = "webhook.verification";

    /// <summary>How far from now a signature may be, 5 minutes, which stops an intercepted delivery from being replayed.</summary>
    public static readonly TimeSpan DefaultTolerance = TimeSpan.FromSeconds(300);

    /// <summary>Verify a delivery's signature and parse its payload.</summary>
    /// <param name="payload">The raw request body, exactly as received.</param>
    /// <param name="signatureHeader">The X-VeliraPay-Signature header.</param>
    /// <param name="secret">The endpoint's signing secret, starting with "whsec_".</param>
    /// <param name="tolerance">How far from now a signature may be; <see cref="DefaultTolerance"/> when null, and <see cref="TimeSpan.Zero"/> accepts any age.</param>
    /// <exception cref="SignatureVerificationException">The signature is missing, does not match, or is too old.</exception>
    /// <exception cref="InvalidArgumentException">The secret is missing, the tolerance is negative, or the payload is not a JSON object.</exception>
    public static WebhookEvent ConstructEvent(string payload, string? signatureHeader, string? secret, TimeSpan? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return ConstructEvent(Encoding.UTF8.GetBytes(payload), signatureHeader, secret, tolerance);
    }

    /// <summary>Verify a delivery's signature and parse its payload.</summary>
    /// <param name="payload">The raw request body, exactly as received.</param>
    /// <param name="signatureHeader">The X-VeliraPay-Signature header.</param>
    /// <param name="secret">The endpoint's signing secret, starting with "whsec_".</param>
    /// <param name="tolerance">How far from now a signature may be; <see cref="DefaultTolerance"/> when null, and <see cref="TimeSpan.Zero"/> accepts any age.</param>
    /// <exception cref="SignatureVerificationException">The signature is missing, does not match, or is too old.</exception>
    /// <exception cref="InvalidArgumentException">The secret is missing, the tolerance is negative, or the payload is not a JSON object.</exception>
    public static WebhookEvent ConstructEvent(ReadOnlySpan<byte> payload, string? signatureHeader, string? secret, TimeSpan? tolerance = null)
    {
        VerifySignature(payload, signatureHeader, secret, tolerance);

        return WebhookEvent.FromPayload(payload);
    }

    /// <summary>Read a delivery's body from a stream, such as ASP.NET Core's <c>Request.Body</c>, then verify its signature and parse it.</summary>
    /// <param name="body">The request body, unread.</param>
    /// <param name="signatureHeader">The X-VeliraPay-Signature header.</param>
    /// <param name="secret">The endpoint's signing secret, starting with "whsec_".</param>
    /// <param name="tolerance">How far from now a signature may be; <see cref="DefaultTolerance"/> when null, and <see cref="TimeSpan.Zero"/> accepts any age.</param>
    /// <param name="cancellationToken">Cancels reading the body.</param>
    /// <exception cref="SignatureVerificationException">The signature is missing, does not match, or is too old.</exception>
    /// <exception cref="InvalidArgumentException">The secret is missing, the tolerance is negative, or the payload is not a JSON object.</exception>
    public static async Task<WebhookEvent> ConstructEventAsync(Stream body, string? signatureHeader, string? secret, TimeSpan? tolerance = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(body);

        using var buffer = new MemoryStream();
        await body.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

        return ConstructEvent(buffer.GetBuffer().AsSpan(0, (int)buffer.Length), signatureHeader, secret, tolerance);
    }

    /// <summary>Check that a payload was signed with the secret, recently. Every v1 in the header is tried, so a delivery signed with the old secret during a rotation passes too.</summary>
    /// <param name="payload">The raw request body, exactly as received.</param>
    /// <param name="signatureHeader">The X-VeliraPay-Signature header.</param>
    /// <param name="secret">The endpoint's signing secret, starting with "whsec_".</param>
    /// <param name="tolerance">How far from now a signature may be; <see cref="DefaultTolerance"/> when null, and <see cref="TimeSpan.Zero"/> accepts any age.</param>
    /// <param name="now">The current time, for tests; the clock's when null.</param>
    /// <exception cref="SignatureVerificationException">The signature is missing, does not match, or is too old.</exception>
    /// <exception cref="InvalidArgumentException">The secret is missing or the tolerance is negative.</exception>
    public static void VerifySignature(string payload, string? signatureHeader, string? secret, TimeSpan? tolerance = null, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(payload);

        VerifySignature(Encoding.UTF8.GetBytes(payload), signatureHeader, secret, tolerance, now);
    }

    /// <summary>Check that a payload was signed with the secret, recently. Every v1 in the header is tried, so a delivery signed with the old secret during a rotation passes too.</summary>
    /// <param name="payload">The raw request body, exactly as received.</param>
    /// <param name="signatureHeader">The X-VeliraPay-Signature header.</param>
    /// <param name="secret">The endpoint's signing secret, starting with "whsec_".</param>
    /// <param name="tolerance">How far from now a signature may be; <see cref="DefaultTolerance"/> when null, and <see cref="TimeSpan.Zero"/> accepts any age.</param>
    /// <param name="now">The current time, for tests; the clock's when null.</param>
    /// <exception cref="SignatureVerificationException">The signature is missing, does not match, or is too old.</exception>
    /// <exception cref="InvalidArgumentException">The secret is missing or the tolerance is negative.</exception>
    public static void VerifySignature(ReadOnlySpan<byte> payload, string? signatureHeader, string? secret, TimeSpan? tolerance = null, DateTimeOffset? now = null)
    {
        var allowed = tolerance ?? DefaultTolerance;

        if (allowed < TimeSpan.Zero)
        {
            throw new InvalidArgumentException("The webhook signature tolerance cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidArgumentException("A webhook signing secret is required. Copy it from the webhook endpoint in the dashboard.");
        }

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            throw new SignatureVerificationException($"The {SignatureHeader} header is missing.");
        }

        string? timestamp = null;
        var signatures = new List<string>();

        foreach (var part in signatureHeader.Split(','))
        {
            var trimmed = part.Trim();
            var separator = trimmed.IndexOf('=', StringComparison.Ordinal);
            var key = separator < 0 ? trimmed : trimmed[..separator];
            var value = separator < 0 ? string.Empty : trimmed[(separator + 1)..];

            if (key == "t" && timestamp is null)
            {
                timestamp = value;
            }
            else if (key == "v1" && value.Length > 0)
            {
                signatures.Add(value);
            }
        }

        if (timestamp is not { Length: > 0 } || !timestamp.All(char.IsAsciiDigit))
        {
            throw new SignatureVerificationException($"The {SignatureHeader} header has no timestamp.");
        }

        if (signatures.Count == 0)
        {
            throw new SignatureVerificationException($"The {SignatureHeader} header has no v1 signature.");
        }

        var age = BigInteger.Abs(new BigInteger((now ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds()) - BigInteger.Parse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture));

        if (allowed > TimeSpan.Zero && (double)age > allowed.TotalSeconds)
        {
            throw new SignatureVerificationException($"The signature is {age} seconds away from now, more than the {allowed.TotalSeconds.ToString(CultureInfo.InvariantCulture)} allowed.");
        }

        var expected = Sign(payload, secret, timestamp);

        foreach (var signature in signatures)
        {
            if (CryptographicOperations.FixedTimeEquals(expected, Encoding.UTF8.GetBytes(signature)))
            {
                return;
            }
        }

        throw new SignatureVerificationException("The signature does not match the payload. Check the signing secret and that the raw request body is passed unchanged.");
    }

    /// <summary>Build the signature header VeliraPay would send with a payload, to test a webhook handler.</summary>
    /// <param name="payload">The body to sign.</param>
    /// <param name="secret">The signing secret.</param>
    /// <param name="timestamp">When it was signed; now when null.</param>
    public static string GenerateSignatureHeader(string payload, string secret, DateTimeOffset? timestamp = null)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return GenerateSignatureHeader(Encoding.UTF8.GetBytes(payload), secret, timestamp);
    }

    /// <summary>Build the signature header VeliraPay would send with a payload, to test a webhook handler.</summary>
    /// <param name="payload">The body to sign.</param>
    /// <param name="secret">The signing secret.</param>
    /// <param name="timestamp">When it was signed; now when null.</param>
    public static string GenerateSignatureHeader(ReadOnlySpan<byte> payload, string secret, DateTimeOffset? timestamp = null)
    {
        ArgumentNullException.ThrowIfNull(secret);

        var time = (timestamp ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

        return $"t={time},v1={Encoding.ASCII.GetString(Sign(payload, secret, time))}";
    }

    /// <summary>The HMAC-SHA256, in lowercase hex, of "{timestamp}.{payload}" under the secret.</summary>
    private static byte[] Sign(ReadOnlySpan<byte> payload, string secret, string timestamp)
    {
        using var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(secret));
        hmac.AppendData(Encoding.ASCII.GetBytes($"{timestamp}."));
        hmac.AppendData(payload);

        return Encoding.ASCII.GetBytes(Convert.ToHexStringLower(hmac.GetHashAndReset()));
    }
}
