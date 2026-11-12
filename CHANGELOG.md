# Changelog

All notable changes to this library are documented here. It follows [Semantic Versioning](https://semver.org).

## 0.1.0 - Unreleased

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
