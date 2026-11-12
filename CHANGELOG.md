# Changelog

All notable changes to this library are documented here. It follows [Semantic Versioning](https://semver.org).

## Unreleased

- Constants for more of the values the API names, as `static` classes of strings like `Asset`: `AddressNetwork`, the networks `PayoutAddresses.UpdateAsync()` and `DeleteAsync()` take (only those `PayoutAddresses.ListAsync()` returns can be set; the API answers the others with a 404), `CheckoutAppearance`, `WebhookDeliveryStatus`, `PlanName`, `AccountType`, `CheckoutLocale`, the checkout's eight languages with Arabic, and `ActivityAction`, every action of the activity log, `team.second_factor_requirement_changed` included. Plain strings keep working wherever these are taken or given.
- The overview is typed whole: `Period` (`OverviewPeriod`, its first and last day), `Funnel` (`OverviewFunnel`, how the attempts to pay ended, per coin too as `OverviewFunnelAsset`), `Attention` (`OverviewAttention`: underpaid charges and late payments as `OverviewChargeAmount`, payouts waiting for an address as `OverviewWaitingPayouts`, and failing webhook endpoints), `Assets` (`OverviewAsset`), `TopPaymentLinks` (`OverviewPaymentLink`), `Setup` (`OverviewSetup`) and `Daily` (`OverviewDay`), with amounts as `decimal` and days as `DateOnly`, and `Mode`, the key's mode the answer names. `Get()` still reads them as the API sent them.
- `Page<T>.Meta`, everything a list's answer said under `meta`, with `Mode`, the key's mode that the coins on offer and the activity log name, and `Currencies`, the currencies prices can be given in, which the coins on offer carry. `PayoutAddresses.ListAsync()` and `Payouts.SummaryAsync()` still answer with plain lists, whose answers' meta `LastResponse` holds; the mode is `VeliraPayClient.Mode`, and whether an owner lets API keys change payout addresses is `Account.ApiPayoutAddressChanges`.

## 0.1.0 - 2026-11-12

The first version, covering what the PHP library covers as of its 0.6.5, and what the Node.js library 0.1.0 adds:

- `VeliraPayClient` with `Account`, `Assets`, `Charges`, `PaymentLinks`, `Invoices`, `Coupons`, `Payouts`, `PayoutAddresses`, `Events`, `WebhookEndpoints`, `Overview`, `Activity`, `ApiKeys`, `Members`, `Invitations` and `Billing`, async throughout with a `CancellationToken` on every call, answering with typed objects that keep every field the API sent (`Get()`, `RawJson`, `ToJson()`).
- Typed options for every request, sent under the API's own names, with only the properties that are set; `AddExtraParam()` for a field the library has no property for yet.
- `decimal` for every amount, read with every place the API wrote and sent as a decimal string without trailing zeros, so no precision is lost and a computed amount is not refused for its places; `DateTimeOffset` for times and `DateOnly` for days.
- `Page<T>` for lists, with `NextPageAsync()` and `AutoPagingIteratorAsync()`, and `IteratorAsync()` on every paged list for `await foreach`.
- An exception per kind of failure, all extending `VeliraPayException`: `AuthenticationException`, `PermissionException`, `NotFoundException`, `ConflictException`, `ValidationException`, `RateLimitException`, `ServerException`, `InvalidRequestException`, `UnexpectedResponseException`, `ConnectionException`, `SignatureVerificationException` and `InvalidArgumentException`.
- Retries of network errors, timeouts, rate limits and outages, with an `Idempotency-Key` on every write so a retried one is carried out once.
- `Webhook.ConstructEvent()` and `ConstructEventAsync()`, which check the signature, the second one sent for a day after a secret is rotated, and its age.
- `Asset` has `BTC_LIGHTNING`, `ETH_BASE`, `ETH_ARBITRUM` and `POL` too, and charges and coins read the `network_surcharge` the API sends, and charges its `payment_request`.
- An injectable `HttpClient`, for `IHttpClientFactory`, and no dependencies beyond .NET 10.
