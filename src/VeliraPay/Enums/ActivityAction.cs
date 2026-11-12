namespace VeliraPay;

/// <summary>
/// What an entry of the activity log records, as <see cref="Activity.Action"/> gives it and <see cref="ActivityListOptions.Action"/> takes it.
/// The ones marked sensitive touch money or access, and are what the log lists with <see cref="ActivityListOptions.Action"/> set to "sensitive".
/// An entry can carry an action the API added after this version of the library, so compare with these values rather than assume the list is complete.
/// </summary>
public static class ActivityAction
{
    /// <summary>The payout addresses were changed. Sensitive.</summary>
    public const string WalletsChanged = "wallets.changed";

    /// <summary>An owner changed whether API keys and AI apps may change the payout addresses. Sensitive.</summary>
    public const string PayoutAddressAccessChanged = "wallets.api_access_changed";

    /// <summary>An owner changed whether everyone must use a passkey or two-factor authentication. Sensitive.</summary>
    public const string SecondFactorRequirementChanged = "team.second_factor_requirement_changed";

    /// <summary>The payment settings were changed.</summary>
    public const string PaymentSettingsUpdated = "payment_settings.updated";

    /// <summary>The public profile was changed.</summary>
    public const string ProfileUpdated = "profile.updated";

    /// <summary>The business details were changed.</summary>
    public const string BusinessDetailsUpdated = "business_details.updated";

    /// <summary>An API key was created. Sensitive.</summary>
    public const string ApiKeyCreated = "api_key.created";

    /// <summary>An API key's name, permissions or allowed addresses were changed. Sensitive.</summary>
    public const string ApiKeyUpdated = "api_key.updated";

    /// <summary>An API key was revoked. Sensitive.</summary>
    public const string ApiKeyRevoked = "api_key.revoked";

    /// <summary>A webhook endpoint was added.</summary>
    public const string WebhookCreated = "webhook.created";

    /// <summary>A webhook endpoint was changed.</summary>
    public const string WebhookUpdated = "webhook.updated";

    /// <summary>A webhook endpoint was deleted.</summary>
    public const string WebhookDeleted = "webhook.deleted";

    /// <summary>A webhook endpoint's signing secret was rotated. Sensitive.</summary>
    public const string WebhookSecretRotated = "webhook.secret_rotated";

    /// <summary>Someone was invited to the account. Sensitive.</summary>
    public const string MemberInvited = "member.invited";

    /// <summary>Someone joined the account. Sensitive.</summary>
    public const string MemberJoined = "member.joined";

    /// <summary>A member's role was changed. Sensitive.</summary>
    public const string MemberRoleChanged = "member.role_changed";

    /// <summary>A member was removed. Sensitive.</summary>
    public const string MemberRemoved = "member.removed";

    /// <summary>A payment link was created.</summary>
    public const string PaymentLinkCreated = "payment_link.created";

    /// <summary>A payment link was changed.</summary>
    public const string PaymentLinkUpdated = "payment_link.updated";

    /// <summary>A payment link was archived.</summary>
    public const string PaymentLinkArchived = "payment_link.archived";

    /// <summary>A payment link was restored.</summary>
    public const string PaymentLinkRestored = "payment_link.restored";

    /// <summary>An invitation was canceled. Sensitive.</summary>
    public const string InvitationCanceled = "member.invitation_canceled";

    /// <summary>A customer was emailed a charge's payment page.</summary>
    public const string ChargePaymentRequested = "charge.payment_requested";

    /// <summary>A webhook endpoint was switched off after its deliveries kept failing. Sensitive.</summary>
    public const string WebhookDisabled = "webhook.disabled";

    /// <summary>A charge was canceled.</summary>
    public const string ChargeCanceled = "charge.canceled";

    /// <summary>A short or late payment was accepted as paid.</summary>
    public const string ChargeAccepted = "charge.accepted";

    /// <summary>A refund was recorded.</summary>
    public const string RefundRecorded = "refund.recorded";

    /// <summary>An invoice was voided.</summary>
    public const string InvoiceVoided = "invoice.voided";

    /// <summary>Paying for Pro in crypto was started.</summary>
    public const string PlanPaymentStarted = "plan.payment_started";

    /// <summary>The Pro subscription was canceled.</summary>
    public const string SubscriptionCanceled = "plan.subscription_canceled";

    /// <summary>The Pro subscription was resumed.</summary>
    public const string SubscriptionResumed = "plan.subscription_resumed";

    /// <summary>A member signed in.</summary>
    public const string SignedIn = "auth.signed_in";

    /// <summary>A member could not sign in. Sensitive.</summary>
    public const string SignInFailed = "auth.sign_in_failed";

    /// <summary>A member changed their password. Sensitive.</summary>
    public const string PasswordChanged = "auth.password_changed";

    /// <summary>A member turned on two-factor authentication. Sensitive.</summary>
    public const string TwoFactorEnabled = "auth.two_factor_enabled";

    /// <summary>A member turned off two-factor authentication. Sensitive.</summary>
    public const string TwoFactorDisabled = "auth.two_factor_disabled";

    /// <summary>A member signed out of every other browser. Sensitive.</summary>
    public const string SignedOutElsewhere = "auth.signed_out_elsewhere";

    /// <summary>A member gave a wrong two-factor code. Sensitive.</summary>
    public const string TwoFactorFailed = "auth.two_factor_failed";

    /// <summary>A member changed their email address. Sensitive.</summary>
    public const string EmailChanged = "auth.email_changed";

    /// <summary>A member added a passkey. Sensitive.</summary>
    public const string PasskeyAdded = "auth.passkey_added";

    /// <summary>A member removed a passkey. Sensitive.</summary>
    public const string PasskeyRemoved = "auth.passkey_removed";

    /// <summary>A coupon was created.</summary>
    public const string CouponCreated = "coupon.created";

    /// <summary>A coupon was changed.</summary>
    public const string CouponUpdated = "coupon.updated";

    /// <summary>A coupon was deleted.</summary>
    public const string CouponDeleted = "coupon.deleted";

    /// <summary>An AI app was connected over OAuth. Sensitive.</summary>
    public const string AppConnected = "auth.app_connected";

    /// <summary>An AI app was disconnected. Sensitive.</summary>
    public const string AppDisconnected = "auth.app_disconnected";

    /// <summary>Chat notifications were connected.</summary>
    public const string ChatConnected = "chat.connected";

    /// <summary>Chat notifications were changed.</summary>
    public const string ChatUpdated = "chat.updated";

    /// <summary>Chat notifications were disconnected.</summary>
    public const string ChatRemoved = "chat.removed";

    /// <summary>Chat notifications were switched off because the chat stopped taking them. Sensitive.</summary>
    public const string ChatDisabled = "chat.disabled";

    /// <summary>A payment came from an address on the sanctions list. Sensitive.</summary>
    public const string SanctionsMatched = "sanctions.matched";

    /// <summary>A payment from an address on the sanctions list was accepted. Sensitive.</summary>
    public const string SanctionsAccepted = "sanctions.accepted";

    /// <summary>A payment from an address on the sanctions list was asked to be returned. Sensitive.</summary>
    public const string SanctionsReturnRequested = "sanctions.return_requested";

    /// <summary>A member connected an account to sign in with, such as Google or GitHub. Sensitive.</summary>
    public const string SocialAccountConnected = "auth.social_connected";

    /// <summary>A member disconnected an account they signed in with. Sensitive.</summary>
    public const string SocialAccountDisconnected = "auth.social_disconnected";
}
