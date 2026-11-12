namespace VeliraPay;

/// <summary>
/// What a restricted API key may do. Full access gives every permission but the sensitive ones, which a key only has when it is made with them ticked:
/// <see cref="ActivityRead"/>, <see cref="AccountWrite"/>, <see cref="PaymentSettingsWrite"/>, <see cref="PayoutAddressesWrite"/>,
/// <see cref="WebhookEndpointsWrite"/>, <see cref="ApiKeysRead"/>, <see cref="ApiKeysWrite"/>, <see cref="MembersRead"/> and <see cref="MembersWrite"/>.
/// </summary>
public static class ApiKeyPermission
{
    /// <summary>Read charges.</summary>
    public const string ChargesRead = "charges:read";

    /// <summary>Create, email and cancel charges.</summary>
    public const string ChargesWrite = "charges:write";

    /// <summary>Accept short and late payments.</summary>
    public const string ChargesAccept = "charges:accept";

    /// <summary>Record refunds.</summary>
    public const string ChargesRefund = "charges:refund";

    /// <summary>Read invoices.</summary>
    public const string InvoicesRead = "invoices:read";

    /// <summary>Create, email and void invoices.</summary>
    public const string InvoicesWrite = "invoices:write";

    /// <summary>Read payment links.</summary>
    public const string PaymentLinksRead = "payment_links:read";

    /// <summary>Create, change, archive and restore payment links.</summary>
    public const string PaymentLinksWrite = "payment_links:write";

    /// <summary>Read coupons.</summary>
    public const string CouponsRead = "coupons:read";

    /// <summary>Create, change and delete coupons.</summary>
    public const string CouponsWrite = "coupons:write";

    /// <summary>Read payouts.</summary>
    public const string PayoutsRead = "payouts:read";

    /// <summary>Read events, and the bodies of webhook deliveries.</summary>
    public const string EventsRead = "events:read";

    /// <summary>Read webhook endpoints and their deliveries.</summary>
    public const string WebhookEndpointsRead = "webhook_endpoints:read";

    /// <summary>Read the overview.</summary>
    public const string OverviewRead = "overview:read";

    /// <summary>Read payout addresses.</summary>
    public const string PayoutAddressesRead = "payout_addresses:read";

    /// <summary>Read the activity log. Sensitive.</summary>
    public const string ActivityRead = "activity:read";

    /// <summary>Change the account's settings, and read its business details. Sensitive.</summary>
    public const string AccountWrite = "account:write";

    /// <summary>Change the payment settings. Sensitive.</summary>
    public const string PaymentSettingsWrite = "payment_settings:write";

    /// <summary>Set and remove payout addresses, once an owner allowed it. Sensitive.</summary>
    public const string PayoutAddressesWrite = "payout_addresses:write";

    /// <summary>Add, change, verify, test and delete webhook endpoints. Sensitive.</summary>
    public const string WebhookEndpointsWrite = "webhook_endpoints:write";

    /// <summary>Read API keys. Sensitive.</summary>
    public const string ApiKeysRead = "api_keys:read";

    /// <summary>Create, change and revoke API keys. Sensitive.</summary>
    public const string ApiKeysWrite = "api_keys:write";

    /// <summary>Read members and invitations. Sensitive.</summary>
    public const string MembersRead = "members:read";

    /// <summary>Invite, change and remove members. Sensitive.</summary>
    public const string MembersWrite = "members:write";

    /// <summary>Pay for Pro in crypto.</summary>
    public const string BillingWrite = "billing:write";
}
