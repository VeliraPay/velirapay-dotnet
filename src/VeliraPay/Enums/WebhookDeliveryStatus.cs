namespace VeliraPay;

/// <summary>Where a webhook delivery has got to, as <see cref="WebhookDelivery.Status"/> gives it and <see cref="WebhookDeliveryListOptions.Status"/> takes it.</summary>
public static class WebhookDeliveryStatus
{
    /// <summary>Waiting to be sent, or to be sent again.</summary>
    public const string Pending = "pending";

    /// <summary>The endpoint accepted it with a 2xx status.</summary>
    public const string Succeeded = "succeeded";

    /// <summary>Every attempt to send it failed.</summary>
    public const string Failed = "failed";
}
