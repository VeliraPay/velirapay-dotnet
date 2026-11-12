using System.Reflection;
using static VeliraPay.Tests.Support.TestSupport;

namespace VeliraPay.Tests;

/// <summary>The constants for the values the API names, held against its own lists in velirapay.com, which they must be updated with.</summary>
public sealed class ConstantsTests
{
    [Fact]
    public void EveryCurrencyThePricesCanBeInHasAConstant()
    {
        // config('velirapay.currencies')
        Assert.Equal(["USD", "EUR", "GBP", "CAD", "AUD", "CHF", "TRY"], Values(typeof(Currency)));
    }

    [Fact]
    public void EveryRoleHasAConstant()
    {
        // App\Enums\TeamRole
        Assert.Equal(["owner", "admin", "finance", "support", "viewer"], Values(typeof(MemberRole)));
    }

    [Fact]
    public void EveryPayoutNetworkHasAConstant()
    {
        // App\Enums\AddressFormat
        Assert.Equal(
            ["bitcoin", "litecoin", "dogecoin", "bitcoincash", "evm", "bsc", "base", "arbitrum", "polygon", "tron", "monero", "solana", "lightning"],
            Values(typeof(AddressNetwork)));
        Assert.Equal("bitcoincash", AddressNetwork.BitcoinCash);
        Assert.Equal("evm", AddressNetwork.Evm);
    }

    [Fact]
    public void EveryCheckoutAppearanceDeliveryStatusPlanAndAccountTypeHasAConstant()
    {
        // App\Enums\CheckoutAppearance, App\Enums\WebhookDeliveryStatus and App\Enums\Plan, and the types AccountResource gives.
        Assert.Equal(["auto", "light", "dark"], Values(typeof(CheckoutAppearance)));
        Assert.Equal(["pending", "succeeded", "failed"], Values(typeof(WebhookDeliveryStatus)));
        Assert.Equal(["free", "pro"], Values(typeof(PlanName)));
        Assert.Equal(["personal", "organization"], Values(typeof(AccountType)));
    }

    [Fact]
    public void EveryCheckoutLanguageHasAConstant()
    {
        // config('velirapay.locales'), which App\Support\Locales offers at checkout, Arabic included.
        Assert.Equal(["en", "fr", "de", "es", "tr", "pt", "ru", "ar"], Values(typeof(CheckoutLocale)));
        Assert.Equal("pt", CheckoutLocale.Portuguese);
    }

    [Fact]
    public void EveryActivityActionHasAConstant()
    {
        // App\Enums\ActivityAction, in its order.
        Assert.Equal(
            [
                "wallets.changed", "wallets.api_access_changed", "team.second_factor_requirement_changed", "payment_settings.updated", "profile.updated",
                "business_details.updated", "api_key.created", "api_key.updated", "api_key.revoked", "webhook.created", "webhook.updated", "webhook.deleted",
                "webhook.secret_rotated", "member.invited", "member.joined", "member.role_changed", "member.removed", "payment_link.created",
                "payment_link.updated", "payment_link.archived", "payment_link.restored", "member.invitation_canceled", "charge.payment_requested",
                "webhook.disabled", "charge.canceled", "charge.accepted", "refund.recorded", "invoice.voided", "plan.payment_started",
                "plan.subscription_canceled", "plan.subscription_resumed", "auth.signed_in", "auth.sign_in_failed", "auth.password_changed",
                "auth.two_factor_enabled", "auth.two_factor_disabled", "auth.signed_out_elsewhere", "auth.two_factor_failed", "auth.email_changed",
                "auth.passkey_added", "auth.passkey_removed", "coupon.created", "coupon.updated", "coupon.deleted", "auth.app_connected",
                "auth.app_disconnected", "chat.connected", "chat.updated", "chat.removed", "chat.disabled", "sanctions.matched", "sanctions.accepted",
                "sanctions.return_requested", "auth.social_connected", "auth.social_disconnected",
            ],
            Values(typeof(ActivityAction)));
        Assert.Equal("team.second_factor_requirement_changed", ActivityAction.SecondFactorRequirementChanged);
        Assert.Equal("member.invitation_canceled", ActivityAction.InvitationCanceled);
    }

    [Fact]
    public void TheLibrarysOwnChecksUseTheConstants()
    {
        var account = new Account(Element(Json("""{"id":"acme","type":"organization","plan":{"name":"pro"},"checkout":{}}""")));
        var delivery = new WebhookDelivery(Element(Json("""{"id":"1","status":"succeeded"}""")));

        Assert.True(account.IsOrganization);
        Assert.True(account.IsPro);
        Assert.Equal(CheckoutAppearance.Auto, account.Checkout?.Appearance);
        Assert.True(delivery.Succeeded);
        Assert.Equal(PlanName.Free, new Account(Element(Json("""{"id":"acme"}"""))).Plan);
    }

    /// <summary>The values of a class's string constants, in the order they are declared.</summary>
    private static List<string> Values(Type constants)
        => [.. constants.GetFields(BindingFlags.Public | BindingFlags.Static).Where(field => field.IsLiteral).OrderBy(field => field.MetadataToken).Select(field => (string)field.GetRawConstantValue()!)];
}
