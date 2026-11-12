# VeliraPay for .NET

[![Tests](https://github.com/VeliraPay/velirapay-dotnet/actions/workflows/tests.yml/badge.svg)](https://github.com/VeliraPay/velirapay-dotnet/actions/workflows/tests.yml)
[![NuGet](https://img.shields.io/nuget/v/VeliraPay)](https://www.nuget.org/packages/VeliraPay)
[![License](https://img.shields.io/github/license/VeliraPay/velirapay-dotnet)](https://github.com/VeliraPay/velirapay-dotnet/blob/main/LICENSE)

The official .NET library for [VeliraPay](https://velirapay.com), the crypto payment processor. It covers the v1 API (the coins on offer, charges, invoices, payment links, coupons, payouts, events, webhook endpoints and your account) and verifies the webhooks VeliraPay sends you. It is async throughout, uses `decimal` for every amount, and has no dependencies beyond .NET itself.

- [Requirements](#requirements)
- [Installation](#installation)
- [Charging a customer](#charging-a-customer)
- [Verifying webhooks](#verifying-webhooks)
- [Which one to use](#which-one-to-use)
- [What the client covers](#what-the-client-covers)
- [Lists and pagination](#lists-and-pagination)
- [Errors](#errors)
- [Retries and idempotency](#retries-and-idempotency)
- [Test mode](#test-mode)
- [Configuration](#configuration)

The full API reference is at [velirapay.com/docs](https://velirapay.com/docs).

## Requirements

.NET 10 or later.

## Installation

```bash
dotnet add package VeliraPay
```

## Charging a customer

Create an API key in the dashboard under **Developers → API keys**. Keep it on your server: anyone holding it can create charges and read your payments.

To charge a customer, create a charge with what you already know about them, and send them to its checkout page:

```csharp
using VeliraPay;

var velirapay = new VeliraPayClient(Environment.GetEnvironmentVariable("VELIRAPAY_API_KEY"));

var charge = await velirapay.Charges.CreateAsync(new ChargeCreateOptions
{
    Amount = 49.90m,
    Currency = Currency.EUR,
    Asset = Asset.BTC,
    Customer = new CustomerOptions { Email = "ada@example.com", Name = "Ada Lovelace" },
    Metadata = new() { ["order_id"] = "1042" },
    SuccessUrl = "https://example.com/orders/1042",
}, new RequestOptions { IdempotencyKey = "order-1042" });

// Send the customer to charge.CheckoutUrl, such as with Results.Redirect(charge.CheckoutUrl!).
```

They pay there, and VeliraPay tells your server about it with a [webhook](#verifying-webhooks). The exchange rate is locked when the charge is created, and the customer has 30 minutes to pay.

Options are typed classes whose properties are sent under the names the API gives them, in snake_case: `SuccessUrl` goes as `success_url`. Only the properties you set are sent; one you set to `null` is sent as `null`, which clears that field in an update. Objects come back as typed classes, such as `charge.AssetAmount` and `charge.Customer.IpAddress`. Every amount is a `decimal`, read with every place the API wrote and sent as a decimal string, so no precision is lost. Trailing zeros are left out, as the API counts every place written: `49.90m * 1.20m` goes as `"59.88"`, not `"59.8800"`. Round an amount to the places a field takes, such as 2 for a price, before sending it. Every time is a `DateTimeOffset`. `CheckoutUrl` is `string?` because the charges in webhooks leave it out; a charge the API answers with always has one.

A charge from your server comes from your server rather than the customer, so give what you know about them as `Customer` (`Email`, `Name`, `IpAddress`, `UserAgent`, `Reference`, `Phone`, `Country` and `Metadata`); it helps you spot fraud before you ship anything. `charge.Customer` reads it back.

## Verifying webhooks

Add an endpoint in the dashboard under **Developers → Webhooks** and copy its signing secret (`whsec_…`). `Webhook.ConstructEventAsync()` reads a delivery's body, checks its signature and parses it. Give it the **raw** body: a body parsed and written out again no longer matches its signature. In an ASP.NET Core minimal API:

```csharp
using VeliraPay;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var webhookSecret = builder.Configuration["VeliraPay:WebhookSecret"];

app.MapPost("/webhooks/velirapay", async (HttpRequest request) =>
{
    WebhookEvent webhookEvent;

    try
    {
        webhookEvent = await Webhook.ConstructEventAsync(request.Body, request.Headers[Webhook.SignatureHeader], webhookSecret, cancellationToken: request.HttpContext.RequestAborted);
    }
    catch (SignatureVerificationException)
    {
        return Results.BadRequest();
    }

    // Sent when the endpoint is added or moved: it receives nothing else until it answers.
    if (webhookEvent.IsVerification)
    {
        return Results.Text(webhookEvent.ChallengeResponse(), "application/json");
    }

    // The same event can arrive more than once: remember this key, and skip the events you have handled.
    var key = webhookEvent.EventId ?? webhookEvent.Id;

    if (webhookEvent.Is(EventType.ChargePaid))
    {
        var orderId = webhookEvent.Charge!.Metadata["order_id"].GetString();
        // Fulfil the order.
    }

    return Results.Ok();
});

app.Run();
```

Where you already hold the body, `Webhook.ConstructEvent(payload, signatureHeader, secret)` takes it as a `string` or as bytes.

Some things to know:

- **Answer the verification challenge.** When an endpoint is added, or its URL changes, VeliraPay sends it a signed `webhook.verification` delivery and sends it nothing else until it answers with HTTP 200 and the challenge, as `ChallengeResponse()` gives it. Endpoint URLs must use `https://`.
- **A second signature for a day after a rotation.** For a day after you rotate the signing secret, the `X-VeliraPay-Signature` header carries a `v1` made with the old secret too, so a receiver that still has the old one keeps working. `ConstructEvent()` accepts a delivery whichever `v1` matches.
- **Signatures expire.** A signature more than 5 minutes from the current time is rejected, which stops an intercepted delivery from being replayed. Change the limit with the `tolerance` argument; `TimeSpan.Zero` turns the age check off.
- **Answer quickly.** A delivery counts as delivered when your endpoint answers with a 2xx status within 15 seconds, so queue slow work. Redirects are not followed.
- **Failed deliveries are retried.** A delivery is tried 14 times over about 3 days.
- **Skip duplicates by event.** A retried delivery keeps its id (`webhookEvent.Id`), but each endpoint gets its own copy of an event under its own id. `webhookEvent.EventId` is the same on every copy, so `EventId ?? Id` is the key to remember.
- **Fulfil once.** A paid invoice sends both `charge.paid`, for the charge that paid it, and `invoice.paid`. Fulfil on one of them.
- **Deliveries can arrive out of order.** When the order matters, fetch the current state with `velirapay.Charges.RetrieveAsync(webhookEvent.Charge!.Id)`.

`charge.payment_detected` arrives as soon as a transfer is seen, before it is confirmed: tell the customer, but do not fulfil. It and `charge.late_payment` carry that transfer as `webhookEvent.Transaction`; `payout.sent` and `payout.confirmed` carry the payout as `webhookEvent.Payout`.

To test your handler, sign a payload yourself: `Webhook.GenerateSignatureHeader(payload, "whsec_test")` gives the header VeliraPay would send.

## Which one to use

| When | Use | |
| --- | --- | --- |
| You are asking one customer for one payment | A charge | The default. It has one checkout page for that one payment, with the rate locked for 30 minutes. |
| The customer should choose the coin, or pays later | An invoice | It needs their name and email, has no timer, can be itemised and reminded, and lets them pick the coin on its page. |
| Many customers pay through the same public page | A payment link, created once | A product sold again and again, a tip jar, a link on your site. |

Do not create a new payment link for each customer or each order.

A charge needs its coin up front, because its exchange rate, amount and deposit address depend on it. Ask the customer on your own page, and offer them the coins from `velirapay.Assets.ListAsync()`, which also gives the lowest price each can be charged at.

## What the client covers

| Property | Methods |
| --- | --- |
| `Charges` | `CreateAsync`, `RetrieveAsync`, `ListAsync`, `IteratorAsync`, `SendAsync`, `CancelAsync`, `AcceptAsync`, `RecordRefundAsync`, `SimulateAsync` |
| `Invoices` | `CreateAsync`, `RetrieveAsync`, `ListAsync`, `IteratorAsync`, `SendAsync`, `VoidAsync` |
| `PaymentLinks` | `CreateAsync`, `RetrieveAsync`, `UpdateAsync`, `ListAsync`, `IteratorAsync`, `ArchiveAsync`, `RestoreAsync` |
| `Coupons` | `CreateAsync`, `RetrieveAsync`, `UpdateAsync`, `ListAsync`, `IteratorAsync`, `DeleteAsync` |
| `Assets` | `ListAsync` |
| `Payouts` | `RetrieveAsync` (by the charge's id), `ListAsync`, `IteratorAsync`, `SummaryAsync` |
| `PayoutAddresses` | `ListAsync`, `UpdateAsync`, `DeleteAsync` |
| `Events` | `RetrieveAsync`, `ListAsync`, `IteratorAsync` |
| `WebhookEndpoints` | `CreateAsync`, `RetrieveAsync`, `UpdateAsync`, `DeleteAsync`, `ListAsync`, `IteratorAsync`, `VerifyAsync`, `TestAsync`, `RotateSecretAsync`, `DeliveriesAsync`, `RetrieveDeliveryAsync`, `RetryDeliveryAsync` |
| `Account` | `RetrieveAsync`, `UpdateAsync`, `UpdatePaymentSettingsAsync` |
| `Overview` | `RetrieveAsync` |
| `Activity` | `ListAsync`, `IteratorAsync` |
| `ApiKeys` | `CreateAsync`, `UpdateAsync`, `RevokeAsync`, `ListAsync`, `IteratorAsync` |
| `Members` | `UpdateAsync`, `RemoveAsync`, `ListAsync`, `IteratorAsync` |
| `Invitations` | `CreateAsync`, `CancelAsync`, `ListAsync`, `IteratorAsync` |
| `Billing` | `StartProPaymentAsync` |

Every method takes a `CancellationToken`, and those that change something a `RequestOptions` with an idempotency key. Each answers with an object such as a `Charge`. Values the API names, such as statuses, coins and events, are strings with constants in `ChargeStatus`, `Asset`, `EventType` and their siblings, so a value the API adds later still comes through, and objects have helpers such as `charge.IsPaid`. `Get("field")` reads a field this version of the library has no property for, `RawJson` is the whole object as a `JsonElement`, and `ToJson()` gives it back as the API sent it. To send a field the options have no property for, use `options.AddExtraParam("field", value)`.

Most endpoints need a permission, which a key with full access has, except the sensitive ones, such as `webhook_endpoints:write` and `api_keys:write`: only a key made with them ticked has those. A key without the permission an endpoint needs gets a `PermissionException` naming it.

For an endpoint the library has no method for yet, send the request yourself: `await velirapay.RequestAsync(HttpMethod.Get, "/v1/charges", new Dictionary<string, object?> { ["per_page"] = 5 })` answers with the response, whose `Json()` decodes it.

## Lists and pagination

`ListAsync()` methods answer with one page, newest first (members oldest first). Set `Page` and `PerPage` (up to 100, 25 by default):

```csharp
var page = await velirapay.Charges.ListAsync(new ChargeListOptions { Status = ChargeStatus.Paid, PerPage = 100 });

foreach (var charge in page)
{
    // …
}

var total = page.Total;
var next = page.HasMore ? await page.NextPageAsync() : null;
```

`IteratorAsync()` walks every page, fetching each one as it is reached:

```csharp
await foreach (var charge in velirapay.Charges.IteratorAsync(new ChargeListOptions { Status = ChargeStatus.Paid }))
{
    // …
}
```

`Assets.ListAsync()` answers with a single page holding every coin, and `PayoutAddresses.ListAsync()` and `Payouts.SummaryAsync()` with lists.

## Errors

A failed request throws an exception carrying the API's message. They all extend `VeliraPayException`:

| Exception | When |
| --- | --- |
| `AuthenticationException` | The API key is missing, wrong or revoked (401). |
| `PermissionException` | The API key may not do this (403): a restricted key without the permission the endpoint needs, or a key used from an IP address it does not allow. |
| `NotFoundException` | The object does not exist, or belongs to the other mode (404). |
| `ConflictException` | The object cannot make that change (409), such as canceling a charge that is no longer pending, or an idempotency key reused for a different request. |
| `ValidationException` | The request has invalid fields (422). `Errors` lists them by field, and `FirstError("amount")` gives the first message for one. |
| `RateLimitException` | Too many requests (429). `RetryAfter` says how long to wait. |
| `ServerException` | VeliraPay had a problem (5xx). |
| `InvalidRequestException` | Any other answer that is not a success, such as a 400. |
| `UnexpectedResponseException` | A successful answer the library cannot read. |
| `ConnectionException` | The API could not be reached, or did not answer in time, after any retries. |
| `SignatureVerificationException` | A webhook's signature is missing, does not match, or is too old. |
| `InvalidArgumentException` | The library was given something it cannot use, such as an API key that does not start with `vp_live_` or `vp_test_`. |

Every exception from an API answer extends `ApiException`, with `Status`, `Body` (the decoded response), `Header(name)`, `RetryAfter` and the `Response` itself. A canceled `CancellationToken` throws an `OperationCanceledException`, as usual in .NET.

```csharp
try
{
    await velirapay.Charges.CreateAsync(new ChargeCreateOptions { Amount = 0m, Currency = Currency.EUR, Asset = Asset.BTC });
}
catch (ValidationException exception)
{
    var errors = exception.Errors; // { "amount": ["…"] }
}
catch (ApiException exception)
{
    var status = exception.Status;
}
```

## Retries and idempotency

Requests that fail with a network error, a timeout, a rate limit (429) or an outage (502, 503, 504) are tried again up to twice, waiting a little longer each time, or as long as the API's `Retry-After` header asks. A 500 is retried for reads only, since a write may have been carried out. A `Retry-After` longer than `MaxRetryAfter` (10 seconds unless [configured](#configuration) otherwise) is not waited out: the exception is thrown straight away, and its `RetryAfter` says how long to wait.

While retries are on, each write carries an `Idempotency-Key` header, so the API carries out a retried request once. The library makes up a key per call; give your own, such as an order number, to make a call safe to repeat across processes:

```csharp
var charge = await velirapay.Charges.CreateAsync(options, new RequestOptions { IdempotencyKey = "order-1042" });
```

For 24 hours, a request repeating a key gets the first successful response, and `velirapay.LastResponse?.WasReplayed` is then true. Reusing a key with a different body throws a `ConflictException`. A key covers one account, one mode, one method and one path, and is at most 255 characters.

## Test mode

Keys starting with `vp_test_` work in test mode: charges are paid with test coins on each chain's test network, and nothing touches real funds. Test and live objects are fully separate, and each mode has its own webhooks.

```csharp
var isTest = velirapay.IsTestMode; // read from the key's prefix

// Pay a test charge without any coins: it becomes paid, and the webhooks go out as for a real payment.
await velirapay.Charges.SimulateAsync("k3v9x2m7q8wz");
await velirapay.Charges.SimulateAsync("k3v9x2m7q8wz", new ChargeSimulateOptions { Amount = 0.001m }); // less than is due, to try an underpayment
```

[`examples/ChargeACustomer`](https://github.com/VeliraPay/velirapay-dotnet/tree/main/examples/ChargeACustomer) creates a test charge and reads it back: `VELIRAPAY_API_KEY=vp_test_… dotnet run --project examples/ChargeACustomer`.

## Configuration

```csharp
var velirapay = new VeliraPayClient(apiKey, new VeliraPayClientOptions
{
    MaxRetries = 2,                             // 0 turns retries off
    MaxRetryAfter = TimeSpan.FromSeconds(10),   // the longest Retry-After waited out before trying again
    Timeout = TimeSpan.FromSeconds(30),         // how long one attempt may take
    AppInfo = "MyShop/2.1",                     // added to the User-Agent
});
```

A client is safe to share between threads; keep one for the life of your application. Without an `HttpClient` of your own, every client shares one the library makes, which does not follow redirects and renews its connections every few minutes.

To use `IHttpClientFactory`, give the client the `HttpClient` it makes. Registered as a typed client, it needs no package beyond `Microsoft.Extensions.Http`, which ASP.NET Core already has:

```csharp
builder.Services.AddHttpClient("VeliraPay")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false })
    .AddTypedClient((http, services) => new VeliraPayClient(services.GetRequiredService<IConfiguration>()["VeliraPay:ApiKey"], http));
```

Then ask for a `VeliraPayClient` wherever you need one. The `HttpClient`'s own `Timeout` (100 seconds unless set) applies alongside the library's.

## Development

```bash
dotnet build -warnaserror
dotnet test --solution VeliraPay.slnx
```

The tests run on .NET 10 and .NET 11, so the .NET 11 SDK builds them. They answer every request from memory, with xUnit; nothing reaches the network.

## License

MIT. See [LICENSE](https://github.com/VeliraPay/velirapay-dotnet/blob/main/LICENSE).
