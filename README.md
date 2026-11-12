# VeliraPay for .NET

[![Tests](https://github.com/VeliraPay/velirapay-dotnet/actions/workflows/tests.yml/badge.svg)](https://github.com/VeliraPay/velirapay-dotnet/actions/workflows/tests.yml)
[![NuGet](https://img.shields.io/nuget/v/VeliraPay)](https://www.nuget.org/packages/VeliraPay)
[![License](https://img.shields.io/github/license/VeliraPay/velirapay-dotnet)](https://github.com/VeliraPay/velirapay-dotnet/blob/main/LICENSE)

The official .NET library for [VeliraPay](https://velirapay.com), the crypto payment processor. It covers every endpoint of the v1 API, from charges, invoices, payment links, coupons and payouts to webhook endpoints, API keys, members and your account, and verifies the webhooks VeliraPay sends you. It is async throughout, uses `decimal` for every amount, and has no dependencies beyond .NET itself.

- [Requirements](#requirements)
- [Installation](#installation)
- [Charging a customer](#charging-a-customer)
- [Which one to use](#which-one-to-use)
- [Supported coins](#supported-coins)
- [Charges](#charges)
- [Short and late payments](#short-and-late-payments)
- [Invoices](#invoices)
- [Payment links](#payment-links)
- [Coupons](#coupons)
- [Payouts](#payouts)
- [Payout addresses](#payout-addresses)
- [Events](#events)
- [Verifying webhooks](#verifying-webhooks)
- [Webhook endpoints](#webhook-endpoints)
- [Your account](#your-account)
- [Overview and activity](#overview-and-activity)
- [API keys](#api-keys)
- [Members and invitations](#members-and-invitations)
- [Pro](#pro)
- [What the client covers](#what-the-client-covers)
- [Constants](#constants)
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
}, new RequestOptions { IdempotencyKey = "order-1042-attempt-1" });

// Save charge.Id with the order, then send the customer to charge.CheckoutUrl, such as with Results.Redirect(charge.CheckoutUrl!).
```

They pay there, and VeliraPay tells your server about it with a [webhook](#verifying-webhooks). The exchange rate is locked when the charge is created, and the customer has 30 minutes to pay. The idempotency key makes the call safe to retry: use a new one for a new attempt, such as a charge replacing one that expired ([Retries and idempotency](#retries-and-idempotency)).

Options are typed classes whose properties are sent under the names the API gives them, in snake_case: `SuccessUrl` goes as `success_url`. Only the properties you set are sent; one you set to `null` is sent as `null`, which clears that field in an update. Objects come back as typed classes, such as `charge.AssetAmount` and `charge.Customer.IpAddress`. Every amount is a `decimal`, read with every place the API wrote and sent as a decimal string, so no precision is lost. Trailing zeros are left out, as the API counts every place written: `49.90m * 1.20m` goes as `"59.88"`, not `"59.8800"`. Round an amount to the places a field takes, such as 2 for a price, before sending it. Every time is a `DateTimeOffset`, sent in UTC whatever its offset, and every day a `DateOnly`. `CheckoutUrl` is `string?` because the charges in webhooks leave it out; a charge the API answers with always has one.

## Which one to use

| When | Use | |
| --- | --- | --- |
| You are asking one customer for one payment | A [charge](#charges) | The default. It has one checkout page for that one payment, with the rate locked for 30 minutes. |
| The customer should choose the coin, or pays later | An [invoice](#invoices) | It needs their name and email, has no timer, can be itemised and reminded, and lets them pick the coin on its page. |
| Many customers pay through the same public page | A [payment link](#payment-links), created once | A product sold again and again, a tip jar, a link on your site. |

Do not create a new payment link for each customer or each order.

A charge needs its coin up front, because its exchange rate, amount and deposit address depend on it. Ask the customer on your own page, and offer them the coins from [`velirapay.Assets.ListAsync()`](#supported-coins).

## Supported coins

`Assets.ListAsync()` gives the coins that can be paid in the key's mode, whether your account takes each, and the lowest price a charge in each can have, in one page:

```csharp
var coins = await velirapay.Assets.ListAsync(new AssetListOptions { Currency = Currency.EUR });

foreach (var coin in coins)
{
    Console.WriteLine($"{coin.Id}: {coin.Name} on {coin.Network}"); // "USDT: Tether USD on Ethereum (ERC-20)"; Id is what a charge's Asset takes
    var accepted = coin.Accepted;                 // whether your account takes it, which needs a payout address for its network
    var minimumAmount = coin.MinimumAmount;       // 10, in the coin, or null when it has no minimum of its own
    var minimumPrice = coin.MinimumPrice;         // 8.71, the lowest price in EUR right now
    var networkSurcharge = coin.NetworkSurcharge; // null, or what a charge in it asks on top of the price, in the coin
}

var currencies = coins.Currencies; // ["USD", "EUR", "GBP", …], the currencies prices can be given in
```

Without `Currency`, prices are in your account's default currency.

No charge, payment link or invoice can ask for less than the lowest price of its currency: 0.50 in USD, EUR, GBP and CHF, 0.70 CAD, 0.80 AUD and 25.00 TRY, below which a payment would be mostly network fees. USDT and USDC on Ethereum also start at 10 of the token, because paying one out costs enough in gas that a smaller payment would leave you little; that minimum is in the coin, so `MinimumPrice` moves a little with the market. Below either, `Charges.CreateAsync()` throws a `ValidationException`.

A coin with a `NetworkSurcharge`, such as USDT on Tron (2.50 USDT), asks the customer for that much more than the price, to pay for passing the payment on, so tell them before they choose it. VeliraPay keeps it: it is not part of your payout, and a refund cannot include it. A charge in such a coin counts it in `AssetAmount` and gives it again as `charge.NetworkSurcharge`.

The coins of BNB Chain, Base, Arbitrum One and Polygon can only be paid in live mode. A coin is only offered while VeliraPay can also pay it out, so `Asset` holds codes that are not on offer at a given time: `Assets.ListAsync()` and the account's `AcceptedAssets` say which are.

## Charges

A charge is how you ask a customer for a payment, in one coin.

```csharp
// For an amount you set, with whatever you already know.
var charge = await velirapay.Charges.CreateAsync(new ChargeCreateOptions
{
    Amount = 150.00m,
    Currency = Currency.USD,
    Asset = Asset.USDC, // USD Coin on Ethereum
    Description = "Pro plan, 1 year",
    Metadata = new() { ["order_id"] = "1042" },
    Customer = new CustomerOptions { Email = "ada@example.com", Name = "Ada Lovelace" },
    CustomFields = [new CustomFieldOptions { Label = "Company", Value = "Analytical Engines" }], // shown with the charge in the dashboard
    SuccessUrl = "https://example.com/orders/1042",            // a button back once they have paid
    CancelUrl = "https://example.com/cart",                    // a link back from the checkout
    SuccessMessage = "Thank you, your licence is on its way.", // on the page and in the receipt
    SendEmail = true,                                          // email them the payment page straight away
});

// For one of your payment links: the currency comes from the link, and so does the price when it is fixed.
var linked = await velirapay.Charges.CreateAsync(new ChargeCreateOptions { PaymentLink = "p4n8r2t6y1ua", Asset = Asset.ETH });

// With one of your coupons: FiatAmount is the lower price, and DiscountAmount what the coupon took off.
var discounted = await velirapay.Charges.CreateAsync(new ChargeCreateOptions { PaymentLink = "p4n8r2t6y1ua", Asset = Asset.ETH, Coupon = "SPRING10" });
var discount = discounted.DiscountAmount; // 15.00

var retrieved = await velirapay.Charges.RetrieveAsync("k3v9x2m7q8wz");
var isPaid = retrieved.IsPaid;          // also IsPending, IsUnderpaid, IsExpired, IsCanceled and IsOpen
var toSend = retrieved.AssetAmount;     // 0.0025, what the customer has to send
var received = retrieved.ReceivedAmount; // what arrived so far
var transfers = retrieved.Transactions;  // each with its Txid, Amount, Confirmations and ExplorerUrl

var paid = await velirapay.Charges.ListAsync(new ChargeListOptions { Status = ChargeStatus.Paid });
var hers = await velirapay.Charges.ListAsync(new ChargeListOptions
{
    Search = "ada@example.com", // part of a code, email, name or description, or exactly an address, a transaction hash or an IP address
    Asset = Asset.BTC,
    CreatedFrom = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
});

// Email the customer the payment page, if you did not set SendEmail.
await velirapay.Charges.SendAsync("k3v9x2m7q8wz");

// Only while the charge is pending and no transfer to it has been seen.
await velirapay.Charges.CancelAsync("k3v9x2m7q8wz");
```

`SendAsync()` needs a customer email and only works while the charge is pending, or it throws a `ConflictException`. Creating a charge with `SendEmail` but no customer email throws a `ValidationException` and creates nothing. A charge can be emailed once every 10 minutes, and the accounts of one owner can send 100 emails to customers an hour between them, 10 until one of them has been paid in live mode: charges, invoices and refund notices together. Past either, a `RateLimitException` says how long to wait. In test mode, emails only go to members of your account.

`Metadata` holds up to 20 keys of at most 40 characters each. Values are strings of at most 500 characters, numbers, booleans or `null`.

The accounts of one owner can create 300 charges in each mode within 30 minutes, through the API, the MCP server and the dashboard together, and 30 until one of them has been paid in live mode; paid live charges do not count, and payments customers start on the hosted checkout are not counted. Past that, `CreateAsync()` throws a `RateLimitException` whose `RetryAfter` says how long to wait.

A charge in `Asset.BTC_LIGHTNING`, Bitcoin over Lightning where it is on offer, is paid to a Lightning invoice: `charge.PaymentRequest` holds it (BOLT11), and `DepositAddress` its payment hash. For every other coin `PaymentRequest` is `null`.

### Customer details

`charge.Customer` holds what is known about who pays, to help you spot fraud before you ship anything. When the customer starts the payment on the hosted checkout, VeliraPay records the IP address and browser they did it from. A charge you create from your server comes from your server rather than the customer, so give what you know:

```csharp
var charge = await velirapay.Charges.CreateAsync(new ChargeCreateOptions
{
    Amount = 150.00m,
    Currency = Currency.USD,
    Asset = Asset.BTC,
    Customer = new CustomerOptions
    {
        Email = "ada@example.com",
        Name = "Ada Lovelace",
        IpAddress = "203.0.113.7",     // the customer's own address, such as HttpContext.Connection.RemoteIpAddress
        UserAgent = "Mozilla/5.0 (…)", // their browser's, such as Request.Headers.UserAgent
        Reference = "cus_1042",        // your own id for the customer
        Phone = "+44 20 7946 0958",
        Country = "GB",                // ISO 3166 two-letter code
        Metadata = new() { ["orders"] = 7 }, // the same limits as the charge's metadata
    },
});

var country = charge.Customer.Country; // "GB"; also Email, Name, IpAddress, UserAgent, Reference, Phone and Metadata
```

Every field is optional. The older `CustomerEmail` property still works.

## Short and late payments

A charge is `underpaid` when less than is due arrived. The customer can still top it up until its rate lock ends, and a transfer that reaches it after that, or reaches a charge that expired or was canceled, is a late payment. Both wait for you: accept what arrived as payment, or send it back and record the refund.

```csharp
// Settle an underpaid charge, or a late payment on an expired or canceled one, as paid for what arrived and confirmed.
var accepted = await velirapay.Charges.AcceptAsync("k3v9x2m7q8wz");

// Record a refund you sent from your own wallet. VeliraPay sends nothing itself.
var refunded = await velirapay.Charges.RecordRefundAsync("k3v9x2m7q8wz", new RefundOptions
{
    Amount = 0.0005m,       // in the charge's coin
    Txid = "7d1c5e9a…",     // the transaction that carried it
    Reason = "Returned item",
});

var refunds = refunded.Refunds; // each with its Amount, Txid, Reason and ExplorerUrl; RefundedAmount adds them up
```

`AcceptAsync()` throws a `ConflictException` for any other charge, and for an underpaid one while a payment to it is still confirming. A refund of an underpaid charge or a late payment needs it accepted first, and the network surcharge is never refundable: `RecordRefundAsync()` throws a `ValidationException` otherwise.

Each arrives as the `charge.late_payment` webhook: a transfer to an expired or canceled charge as soon as it is seen, and a late top-up to an underpaid charge once it has confirmed. A transfer on top of a charge already paid sends `charge.late_payment` too, but is counted automatically and waits for nothing.

## Invoices

An invoice is a bill with a hosted page, where the customer pays in the coin of their choice. Use it for a bill to pay later, or when the customer should choose the coin.

```csharp
var invoice = await velirapay.Invoices.CreateAsync(new InvoiceCreateOptions
{
    CustomerName = "Grace Hopper",
    CustomerEmail = "grace@example.com",
    Currency = Currency.USD,
    Items =
    [
        new InvoiceItemOptions { Description = "Consulting", Quantity = 10, UnitAmount = 100.00m },
        new InvoiceItemOptions { Description = "Hosting", Quantity = 1, UnitAmount = 250.00m },
    ],
    DueAt = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
    SendEmail = true,
});

var number = invoice.Number;  // "INV-0042"
var page = invoice.HostedUrl; // where the customer pays

await velirapay.Invoices.SendAsync(invoice.Id); // email it, or send a reminder
await velirapay.Invoices.VoidAsync(invoice.Id);

var overdue = await velirapay.Invoices.ListAsync(new InvoiceListOptions { Overdue = true });
var hers = await velirapay.Invoices.ListAsync(new InvoiceListOptions { Search = "grace@example.com" });
```

Give `Amount` instead of `Items` to bill a single total. An invoice takes no coupon, and the charges made to pay it, one per coin the customer tried, are in `invoice.Charges`.

An invoice can be emailed once every 10 minutes, and its emails count towards the hourly limit under [Charges](#charges); past either, `SendAsync()` throws a `RateLimitException`, and `CreateAsync()` with `SendEmail` throws it and creates nothing. In test mode, invoices are only emailed to members of your account, and anyone else is refused with a `ValidationException`.

## Payment links

A payment link is one reusable public checkout page, for a fixed price or one the customer chooses, opened by many customers who each pick their own coin. Create it once and share it. To charge one customer, [create a charge](#charges) instead.

```csharp
var link = await velirapay.PaymentLinks.CreateAsync(new PaymentLinkCreateOptions
{
    Title = "Pro plan",
    PricingType = PricingType.Fixed,
    Amount = 150.00m,
    Currency = Currency.EUR,
    AcceptedAssets = [Asset.BTC, Asset.ETH, Asset.USDC], // null takes every coin the account takes
    SuccessUrl = "https://example.com/thanks",
    CustomFields = [new CheckoutQuestionOptions { Label = "Company", Required = false }],
});

var url = link.CheckoutUrl; // https://velirapay.com/pay/…

var tipJar = await velirapay.PaymentLinks.CreateAsync(new PaymentLinkCreateOptions
{
    Title = "Tip jar",
    PricingType = PricingType.Open,
    Currency = Currency.USD,
    SuggestedAmounts = [5.00m, 10.00m, 25.00m],
    MinAmount = 1.00m,
});

await velirapay.PaymentLinks.UpdateAsync(link.Id, new PaymentLinkUpdateOptions { Title = "Pro plan (yearly)" });
await velirapay.PaymentLinks.ArchiveAsync(link.Id); // stops taking payments
await velirapay.PaymentLinks.RestoreAsync(link.Id);

var active = await velirapay.PaymentLinks.ListAsync(new PaymentLinkListOptions { Status = PaymentLinkStatus.Active, Search = "Pro plan" }); // Search looks in titles
```

## Coupons

A coupon takes a percentage or a fixed amount off a payment. Customers type its code on the hosted checkout of a payment link, you add it to the link's URL so it is already applied (`https://velirapay.com/pay/p4n8r2t6y1ua?coupon=SPRING10`), or you give it as `Coupon` when you [create a charge](#charges).

```csharp
var coupon = await velirapay.Coupons.CreateAsync(new CouponCreateOptions
{
    Code = "SPRING10",                  // 3 to 32 letters, digits, dashes or underscores, stored in capitals
    PercentOff = 10m,                   // or AmountOff = 5.00m with a Currency
    MaxRedemptions = 100,               // an open charge holds a use until it is paid, expires or is canceled
    OncePerCustomer = true,             // the customer's email is then required to use it
    PaymentLinks = ["p4n8r2t6y1ua"],    // empty for every link, and charges made without one
    ExpiresAt = new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero),
});

var code = coupon.Id;              // "SPRING10": the code is the id, and is matched in any case
var redeemed = coupon.TimesRedeemed; // how many paid charges used it

await velirapay.Coupons.UpdateAsync("SPRING10", new CouponUpdateOptions { Active = false }); // pause it
await velirapay.Coupons.DeleteAsync("SPRING10"); // charges that used it keep its code and their discount

var found = await velirapay.Coupons.ListAsync(new CouponListOptions { Search = "spring" });
```

`Currency` is required with `AmountOff` or `MinAmount`, and the coupon then only works on payments in that currency. A coupon cannot take a payment under the lowest price of its currency. Once a coupon exists, its code, discount, currency and minimum amount cannot change: `UpdateAsync()` takes `MaxRedemptions`, `OncePerCustomer`, `PaymentLinks` (which replaces the whole list), `Active` and `ExpiresAt`.

A coupon that cannot be used makes `Charges.CreateAsync()` throw a `ValidationException` on `coupon` saying why: it does not exist in this mode, is paused, has expired, is limited to other links, is for another currency, needs a higher price, would leave less than the lowest price to pay or is used up, or it is once per customer and the charge has no customer email or that customer already used it.

## Payouts

Every paid charge has one payout: what came in, what was kept, what you are owed, what actually landed and the transaction that carried it. These only read: payouts go out on their own, and nothing here can raise, redirect or reverse one.

```csharp
var payout = await velirapay.Payouts.RetrieveAsync("k3v9x2m7q8wz"); // by the charge's id; a NotFoundException until it is paid

var sent = payout.IsSent;          // confirmed: the money is in your wallet
var gross = payout.GrossAmount;    // what the customer paid
var fee = payout.FeeAmount;        // what was kept
var net = payout.NetAmount;        // what you are owed, before the chain's own fee
var networkFee = payout.NetworkFee; // what the chain charged
var landed = payout.SentAmount;    // what therefore reached you
var txid = payout.PayoutTxid;      // the transaction, and payout.ExplorerUrl its page on a block explorer
var error = payout.LastError;      // why the last try did not work, or why it is held

var failed = await velirapay.Payouts.ListAsync(new PayoutListOptions { Status = PayoutStatus.Failed });
var tether = await velirapay.Payouts.ListAsync(new PayoutListOptions { Asset = Asset.USDT, Search = "0x9a4f" }); // a charge id or a transaction hash

foreach (var coin in await velirapay.Payouts.SummaryAsync())
{
    Console.WriteLine($"{coin.Asset}: {coin.PaidOutAmount} paid out, {coin.InFlightAmount} on its way, {coin.OwedAmount} owed");
    var waiting = coin.WaitingForAddress; // how many payouts wait for you to give an address
}
```

## Payout addresses

Payouts go to the address you keep for each network family, one per family and mode: `bitcoin`, `evm` for Ethereum and its tokens, `bsc` for BNB Chain, `tron` and so on, with constants in `AddressNetwork`.

```csharp
foreach (var address in await velirapay.PayoutAddresses.ListAsync()) // needs payout_addresses:read
{
    Console.WriteLine($"{address.Network}: {address.Address ?? "none, so its coins' payouts wait"} ({string.Join(", ", address.Assets)})");
}

await velirapay.PayoutAddresses.UpdateAsync(AddressNetwork.Bitcoin, new PayoutAddressUpdateOptions { Address = "bc1q…" });
await velirapay.PayoutAddresses.DeleteAsync(AddressNetwork.Bitcoin);
```

`ListAsync()` has one entry per network with coins on offer in the key's mode, and only those can be set or removed: any other network is a `NotFoundException`. The networks of BNB Chain, Base, Arbitrum One and Polygon are only on offer in live mode. Changing an address needs the sensitive `payout_addresses:write` permission and an owner switching on "Let API keys and AI apps change payout addresses" under Payment settings, which `Account.ApiPayoutAddressChanges` reports; until then the API answers with a `PermissionException`. Owners and admins are emailed about every change, and payouts to a new address wait 48 hours once the account has been paid out (`PayoutsHeldUntil`).

## Events

Every change to a charge or an invoice is recorded as an event, such as `charge.paid` or `invoice.viewed`. Webhooks carry the same events.

```csharp
var events = await velirapay.Events.ListAsync(new EventListOptions { Charge = "k3v9x2m7q8wz" });
var detected = await velirapay.Events.ListAsync(new EventListOptions { Type = EventType.ChargePaymentDetected });

var paidEvent = await velirapay.Events.RetrieveAsync("9b1f7a3e-2c4d-4e8f-a6b0-1d2c3e4f5a6b");
var isPaid = paidEvent.Is(EventType.ChargePaid);
var details = paidEvent.Details; // what was recorded, such as the amount and txid of a payment or a refund
var asItIs = paidEvent.Charge;   // the charge as it is now, without its timeline
```

## Verifying webhooks

Add an endpoint in the dashboard under **Developers → Webhooks**, or [with the API](#webhook-endpoints), and keep its signing secret (`whsec_…`). Each delivery is a `POST` with a JSON body and the headers `X-VeliraPay-Signature` (`t={timestamp},v1={signature}`), `X-VeliraPay-Event` (the event type) and `X-VeliraPay-Delivery` (the delivery's id). `Webhook.ConstructEventAsync()` reads a delivery's body, checks its signature and parses it. Give it the **raw** body: a body parsed and written out again no longer matches its signature. In an ASP.NET Core minimal API:

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

    // A sample from "Send test event" or WebhookEndpoints.TestAsync(), whose charge is made up.
    if (webhookEvent.Test)
    {
        return Results.Ok();
    }

    // The same event can arrive more than once: remember this key, and skip the events you have handled.
    var key = webhookEvent.EventId ?? webhookEvent.Id;

    // Charges made on a payment link, in the dashboard or for an invoice carry no order_id of yours.
    if (webhookEvent.Is(EventType.ChargePaid) && webhookEvent.Charge is { } charge && charge.Metadata.TryGetValue("order_id", out var orderId))
    {
        var orderNumber = orderId.ToString();
        // Fulfil the order.
    }

    return Results.Ok();
});

app.Run();
```

Where you already hold the body, `Webhook.ConstructEvent(payload, signatureHeader, secret)` takes it as a `string` or as bytes.

Some things to know:

- **Answer the verification challenge.** When an endpoint is added, or its URL changes, VeliraPay sends it a signed `webhook.verification` delivery and sends it nothing else until it answers with HTTP 200 and the challenge, as `ChallengeResponse()` gives it. Endpoint URLs must use `https://`.
- **Read metadata defensively.** `charge.Metadata` holds only what was given when the charge was created: a charge made on a payment link, in the dashboard or to pay an invoice has none of your keys, and a value can be a number or a boolean rather than a string. Use `TryGetValue`, and `ToString()` rather than `GetString()`, which throws on a number. A delivery that throws answers 500 and is sent again for three days.
- **Test mode has its own endpoints.** An endpoint added in test mode receives only test-mode events, whose `webhookEvent.Mode` is `"test"` and `IsLiveMode` false, and a live one only live events. If one URL serves both, check `IsLiveMode` before shipping anything. `webhookEvent.Test` marks the samples that the dashboard's "Send test event" and `WebhookEndpoints.TestAsync()` send, whose charge is made up and which have no `EventId`.
- **A second signature for a day after a rotation.** For a day after you rotate the signing secret, the `X-VeliraPay-Signature` header carries a `v1` made with the old secret too, so a receiver that still has the old one keeps working. `ConstructEvent()` accepts a delivery whichever `v1` matches.
- **Signatures expire.** A signature more than 5 minutes from the current time is rejected, which stops an intercepted delivery from being replayed. Change the limit with the `tolerance` argument; `TimeSpan.Zero` turns the age check off.
- **Answer quickly.** A delivery counts as delivered when your endpoint answers with a 2xx status within 15 seconds, so queue slow work. Redirects are not followed, so a 3xx counts as a failure: give the endpoint's final URL.
- **Failed deliveries are retried.** A delivery is tried 14 times over about 3 days: after 1 minute, 5 minutes, 30 minutes, 1 hour, 2 hours, 4 hours and 8 hours, then every 10 hours. An endpoint whose deliveries fail 15 times in a row is switched off until you switch it back on, and what it missed is not sent again on its own: [resend it](#webhook-endpoints).
- **Skip duplicates by event.** A retried delivery keeps its id (`webhookEvent.Id`, the `X-VeliraPay-Delivery` header), but each endpoint gets its own copy of an event under its own id. `webhookEvent.EventId` is the same on every copy, and is the event's id in the events API, so `EventId ?? Id` is the key to remember.
- **Fulfil once.** A paid invoice sends both `charge.paid`, for the charge that paid it, and `invoice.paid`. Fulfil on one of them.
- **Deliveries can arrive out of order.** When the order matters, fetch the current state with `velirapay.Charges.RetrieveAsync(webhookEvent.Charge!.Id)`.

`charge.payment_detected` arrives as soon as a transfer is seen, before it is confirmed: tell the customer, but do not fulfil. It and `charge.late_payment` carry that transfer as `webhookEvent.Transaction` ([Short and late payments](#short-and-late-payments)). `payout.sent` arrives when your share of a paid charge has been broadcast, and `payout.confirmed` when the chain has confirmed it; both carry the charge, and the payout as `webhookEvent.Payout`, the same object `Payouts.RetrieveAsync()` answers with. Book the money on `payout.confirmed`: a broadcast payout can still fail and go out again, so `payout.sent` can arrive more than once for a charge.

The charge and invoice in a delivery are not quite the ones the API answers with:

- A charge's coin amounts and exchange rate carry all 18 decimal places (`0.002500000000000000` rather than `0.0025`); as `decimal` values they compare equal.
- A charge has no `Timeline`, `Refunds`, `CheckoutUrl` or `ReceiptUrl`. Its payment link and invoice come with their title and number, as `charge.PaymentLinkTitle` and `charge.InvoiceNumber`, which the API's own answers leave out.
- An invoice has no `Overdue` or `DocumentUrl`; both are `null`.
- `charge.refunded` carries no refund. Read it from the event, with `(await velirapay.Events.RetrieveAsync(webhookEvent.EventId!)).Details`, which holds its `amount`, and `txid` and `reason` when you gave them.

To test your handler, sign a payload yourself: `Webhook.GenerateSignatureHeader(payload, "whsec_test")` gives the header VeliraPay would send.

## Webhook endpoints

List them to see from your code why deliveries are not arriving:

```csharp
foreach (var endpoint in await velirapay.WebhookEndpoints.ListAsync())
{
    Console.WriteLine($"{endpoint.Url}: {(endpoint.IsReceiving ? "receiving" : "not receiving")}, {endpoint.FailedDeliveriesCount} of {endpoint.DeliveriesCount} failed in 30 days");
}

var failures = await velirapay.WebhookEndpoints.DeliveriesAsync("12", new WebhookDeliveryListOptions { Status = WebhookDeliveryStatus.Failed });

// One delivery with every attempt to send it, the latest first.
var delivery = await velirapay.WebhookEndpoints.RetrieveDeliveryAsync("12", failures[0].Id);

foreach (var attempt in delivery.AttemptLog)
{
    Console.WriteLine($"{attempt.Attempt}: {attempt.ResponseStatus?.ToString() ?? attempt.Error} after {attempt.DurationMs} ms");
}

var body = delivery.Payload; // the JSON body it carried, or null for a key without events:read
```

A key with the sensitive `webhook_endpoints:write` permission can also add and change them, up to 10 per mode. A new endpoint is sent its verification challenge at once, and the answer that adds it, or rotates its secret, is the only one that carries the signing secret:

```csharp
var endpoint = await velirapay.WebhookEndpoints.CreateAsync(new WebhookEndpointCreateOptions
{
    Url = "https://example.com/webhooks/velirapay",
    Events = [EventType.ChargePaid, EventType.InvoicePaid],
});

var secret = endpoint.Secret;                 // "whsec_…": store it now, as it is never shown again
var whyNotVerified = endpoint.VerificationError; // null once the endpoint answered the challenge

await velirapay.WebhookEndpoints.UpdateAsync(endpoint.Id, new WebhookEndpointUpdateOptions { Active = false });
await velirapay.WebhookEndpoints.VerifyAsync(endpoint.Id); // send the challenge again
await velirapay.WebhookEndpoints.TestAsync(endpoint.Id);   // a sample charge.paid, marked Test

var rotated = await velirapay.WebhookEndpoints.RotateSecretAsync(endpoint.Id);
var newSecret = rotated.Secret; // the old one keeps signing for a day, until rotated.PreviousSecretExpiresAt

await velirapay.WebhookEndpoints.RetryDeliveryAsync(endpoint.Id, "9d2e0b8a-…");
await velirapay.WebhookEndpoints.DeleteAsync(endpoint.Id);
```

Adding an endpoint, giving one a new URL, `VerifyAsync()`, `TestAsync()` and `RetryDeliveryAsync()` each send the endpoint a request, so together they are limited to 10 a minute per key. Past that the API answers 429: a short wait is [retried](#retries-and-idempotency) for you, and a longer one throws a `RateLimitException` whose `RetryAfter` says how long to wait. Deliveries are kept for 30 days, and webhook deliveries are the one paged list without an `IteratorAsync()` ([Lists and pagination](#lists-and-pagination)).

If the answer that adds an endpoint or rotates its secret is lost, its secret is gone: see [Retries and idempotency](#retries-and-idempotency).

## Your account

```csharp
var account = await velirapay.Account.RetrieveAsync();

var name = account.DisplayName;              // also SupportEmail, SupportPhone, LogoUrl, LogoDarkUrl, BrandColor, TermsUrl and RefundPolicyUrl
var accepts = account.AcceptedAssets;        // ["BTC", "ETH", …], the coins it takes right now, in the key's mode
var mode = account.Mode;                     // "live" or "test"
var plan = account.Plan;                     // "free" or "pro" (PlanName), with FeePercent, ProEndsAt and IsPro
var appearance = account.Checkout?.Appearance; // "auto", "light" or "dark" (CheckoutAppearance), with CoinOrder, DefaultCoin and the defaults
var prefix = account.Invoices?.NumberPrefix; // with Footer and ShowBusinessDetails
var language = account.CheckoutLocale;       // the checkout's fallback language (CheckoutLocale), or null for English
var remotePayoutChanges = account.ApiPayoutAddressChanges; // whether an owner lets API keys and AI apps change payout addresses

var business = account.Business;   // as entered under Business details in the dashboard's settings
var legalName = business.LegalName; // also Type (BusinessType), RegistrationNumber, TaxId, Address, Industry, ProductDescription, MonthlyVolume and Phone
```

Every business field is `null` until it is filled in, and for a key without the sensitive `account:write` permission, which full access does not include. `Type`, `Industry` and `MonthlyVolume` are also `null` for a value the API added after this version of the library, which `business.Get("industry")` still gives.

A key with the sensitive `account:write` permission can change the account, in the shape it is read in, with `null` to clear a field, and one with `payment_settings:write` the payment settings:

```csharp
await velirapay.Account.UpdateAsync(new AccountUpdateOptions
{
    DisplayName = "Acme",
    Checkout = new AccountCheckoutOptions { Appearance = CheckoutAppearance.Dark, CoinOrder = [Asset.BTC, Asset.ETH] },
});

await velirapay.Account.UpdatePaymentSettingsAsync(new PaymentSettingsUpdateOptions
{
    DefaultCurrency = Currency.EUR,
    UnderpaymentTolerance = 1.5m, // how far short, in percent, still counts as paid
    CheckoutLocale = CheckoutLocale.French,
    InvoiceReminders = true,
});
```

## Overview and activity

`Overview.RetrieveAsync()` gives the dashboard's figures for the last 30 days in the key's mode, and what needs attention now. It needs `overview:read`.

```csharp
var overview = await velirapay.Overview.RetrieveAsync();

var volume = overview.Volume;              // in overview.Currency, with VolumeChangePercent against the 30 days before
var conversion = overview.ConversionRatePercent; // the share of the period's paid, expired and canceled charges that were paid; null when none
var abandoned = overview.Funnel.Abandoned; // also Started, Paid, Waiting, Incomplete, and the same per coin in Funnel.Assets

foreach (var shortPayment in overview.Attention.UnderpaidCharges) // and Attention.LatePayments
{
    Console.WriteLine($"{shortPayment.Id}: {shortPayment.ReceivedAmount} of {shortPayment.Amount} {shortPayment.Asset}");
}

var waiting = overview.Attention.PayoutsWaitingForAddress; // per coin, with Count
var failing = overview.Attention.FailingWebhookEndpoints;
var topLinks = overview.TopPaymentLinks;   // Id, Title and PaidCharges, up to four
var shares = overview.Assets;              // per coin, with PaidCharges and SharePercent
var setUp = overview.Setup.PayoutAddress && overview.Setup.WebhookEndpoint; // also PaymentLink, PaidCharge and PublicProfile

foreach (var day in overview.Daily) // from overview.Period.From to overview.Period.To
{
    Console.WriteLine($"{day.Date}: {day.Volume} paid, {day.SentVolume} paid out, {day.PaidCharges} charges");
}
```

The activity log lists what was done in the account and who did it, newest first, in the key's mode and what belongs to none, such as sign-ins. It needs the sensitive `activity:read` permission.

```csharp
var sensitive = await velirapay.Activity.ListAsync(new ActivityListOptions { Action = "sensitive" });
var keys = await velirapay.Activity.ListAsync(new ActivityListOptions { Action = ActivityAction.ApiKeyCreated, From = new DateOnly(2026, 11, 1) });

foreach (var entry in sensitive)
{
    Console.WriteLine($"{entry.CreatedAt}: {entry.Actor} {entry.Description} ({entry.Action}, from {entry.IpAddress})");
}
```

## API keys

Keys come in two kinds: `vp_live_` works in live mode, `vp_test_` in test mode, and neither can see the other's objects. Reading them needs the sensitive `api_keys:read` permission and changing them `api_keys:write`.

```csharp
var apiKey = await velirapay.ApiKeys.CreateAsync(new ApiKeyCreateOptions
{
    Name = "Shop server",
    Permissions = [ApiKeyPermission.ChargesRead, ApiKeyPermission.ChargesWrite], // leave out for full access
    AllowedIps = ["203.0.113.7"],                                                // leave out for anywhere
});

var plaintext = apiKey.Key; // "vp_live_…", shown this once

await velirapay.ApiKeys.UpdateAsync(apiKey.Id, new ApiKeyUpdateOptions { Name = "Shop server (EU)" });
await velirapay.ApiKeys.RevokeAsync(apiKey.Id);
```

A new key is made in the mode of the key that makes it, up to 10 active keys per mode. A key can make or change no key with a permission or an address it does not have itself, nor touch a key that reaches further. Full access gives every permission but the sensitive ones, which a key only has when it was made with them ticked. If the answer that creates a key is lost, the key is gone: see [Retries and idempotency](#retries-and-idempotency).

## Members and invitations

Reading members and invitations needs the sensitive `members:read` permission, and changing them `members:write`. Changes are made in the name of whoever made the key.

```csharp
foreach (var member in await velirapay.Members.ListAsync()) // oldest first
{
    Console.WriteLine($"{member.Name} <{member.Email}>: {member.Role}");
}

await velirapay.Invitations.CreateAsync(new InvitationCreateOptions { Email = "grace@example.com", Role = MemberRole.Admin });
await velirapay.Members.UpdateAsync("3", new MemberUpdateOptions { Role = MemberRole.Finance });
await velirapay.Members.RemoveAsync("3"); // which also revokes the keys they made

var pending = await velirapay.Invitations.ListAsync();
await velirapay.Invitations.CancelAsync(pending[0].Id);
```

Invitations and role changes take `MemberRole.Admin`, `Finance`, `Support` or `Viewer`; nobody is made an owner, and owners are never changed or removed this way. A key an owner or an admin made can invite people and cancel invitations, and only a key an owner made can change a role or remove a member.

## Pro

Pro keeps no fee on any payment. A live key with `billing:write`, which full access includes, can start paying for a month of it in crypto:

```csharp
var payment = await velirapay.Billing.StartProPaymentAsync(new ProPaymentOptions { Asset = Asset.BTC });

var checkout = payment.CheckoutUrl; // where to pay it, with Price, Currency, AssetAmount and ExpiresAt
var isNew = payment.Started;        // false when a payment in that coin was already waiting, which is given instead
```

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

Every method takes a `CancellationToken`, and those that change something a `RequestOptions` with an idempotency key. Each answers with an object such as a `Charge`, and objects have helpers such as `charge.IsPaid`. `Get("field")` reads a field this version of the library has no property for, `RawJson` is the whole object as a `JsonElement`, and `ToJson()` gives it back as the API sent it. To send a field the options have no property for, use `options.AddExtraParam("field", value)`: a `decimal` goes as a string, as amounts should, and a `double` as a JSON number without an exponent.

Most endpoints need a permission, which a key with full access has, except the sensitive ones, such as `webhook_endpoints:write` and `api_keys:write`: only a key made with them ticked has those. A key without the permission an endpoint needs gets a `PermissionException` naming it.

For an endpoint the library has no method for yet, send the request yourself: `await velirapay.RequestAsync(HttpMethod.Get, "/v1/charges", new Dictionary<string, object?> { ["per_page"] = 5 })` answers with the response, whose `Json()` decodes it.

## Constants

Values the API names are strings, with constants in static classes, so a value the API adds later still comes through, and a plain string works wherever a constant does:

| Class | Values |
| --- | --- |
| `Asset` | The coins, each on one network, such as `Asset.BTC`, `Asset.USDT` (Ethereum) and `Asset.USDT_TRON` |
| `Currency` | `USD`, `EUR`, `GBP`, `CAD`, `AUD`, `CHF`, `TRY` |
| `ChargeStatus`, `InvoiceStatus`, `PaymentLinkStatus`, `PayoutStatus`, `WebhookDeliveryStatus` | The states of each, such as `ChargeStatus.Paid` |
| `EventType` | The events of webhooks and the events API, such as `EventType.ChargePaid` |
| `PricingType` | `Fixed` or `Open`, for payment links |
| `AddressNetwork` | The networks payout addresses are kept for, such as `AddressNetwork.Evm` |
| `ApiKeyPermission` | The permissions of a restricted key, such as `ApiKeyPermission.ChargesRead` |
| `MemberRole` | `Owner`, `Admin`, `Finance`, `Support`, `Viewer` |
| `ActivityAction` | The actions of the activity log, such as `ActivityAction.ApiKeyCreated` |
| `Mode` | `Live` or `Test` |
| `PlanName`, `AccountType` | `Free` or `Pro`; `Personal` or `Organization` |
| `CheckoutAppearance`, `CheckoutLocale` | `Auto`, `Light` or `Dark`; the checkout's languages, such as `CheckoutLocale.French` |
| `BusinessType`, `Industry`, `MonthlyVolume` | The business details |

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

`IteratorAsync()` walks every page, fetching each one as it is reached. Every paged list has one but webhook deliveries, whose first page walks on with `AutoPagingIteratorAsync()`:

```csharp
await foreach (var charge in velirapay.Charges.IteratorAsync(new ChargeListOptions { Status = ChargeStatus.Paid }))
{
    // …
}

var deliveries = await velirapay.WebhookEndpoints.DeliveriesAsync("12");

await foreach (var delivery in deliveries.AutoPagingIteratorAsync())
{
    // …
}
```

A page keeps what its answer said under `meta` as `page.Meta`, and reads two parts of it: `page.Mode`, the key's mode, which the coins on offer and the activity log name, and `page.Currencies`, the currencies prices can be given in, which the coins on offer carry. `Assets.ListAsync()` answers with a single page holding every coin, and `PayoutAddresses.ListAsync()` and `Payouts.SummaryAsync()` with plain lists. Their mode is the client's own `velirapay.Mode`, read from the key, and whether an owner lets API keys change payout addresses is `Account.ApiPayoutAddressChanges`; `velirapay.LastResponse` holds the whole answer right after the call.

## Errors

A failed request throws an exception carrying the API's message. They all extend `VeliraPayException`:

| Exception | When |
| --- | --- |
| `AuthenticationException` | The API key is missing, wrong or revoked (401). |
| `PermissionException` | The API key may not do this (403): a restricted key without the permission the endpoint needs, or a key used from an IP address it does not allow. The message names which. |
| `NotFoundException` | The object does not exist, or belongs to the other mode (404). |
| `ConflictException` | The object cannot make that change (409), such as canceling a charge that is no longer pending or accepting one that is neither underpaid nor late, or an idempotency key reused for a different request. `WasReplayed` is true when it answers a write that already succeeded but whose answer carried a secret ([Retries and idempotency](#retries-and-idempotency)). |
| `ValidationException` | The request has invalid fields (422). `Errors` lists them by field, and `FirstError("amount")` gives the first message for one. |
| `RateLimitException` | Too many requests (429), including emails to customers and charges past their limits. `RetryAfter` says how long to wait. |
| `ServerException` | VeliraPay had a problem (5xx), such as no exchange rate being available to create a charge with (503). |
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

Requests that fail with a network error, a timeout, a rate limit (429) or an outage (502, 503, 504) are tried again up to twice, waiting a little longer each time, or as long as the API's `Retry-After` header asks. A 500 is retried for reads only, since a write may have been carried out. A 409 is retried only when it carries a `Retry-After`: that is the API still working on an earlier request with the same idempotency key, and the retry gets that request's answer once it is done. A `Retry-After` longer than `MaxRetryAfter` (10 seconds unless [configured](#configuration) otherwise) is not waited out: the exception is thrown straight away, and its `RetryAfter` says how long to wait.

While retries are on, each write carries an `Idempotency-Key` header, so the API carries out a retried request once. The library makes up a key per call; give your own to make a call safe to repeat across processes, such as after a crash:

```csharp
var charge = await velirapay.Charges.CreateAsync(
    new ChargeCreateOptions { Amount = 49.90m, Currency = Currency.EUR, Asset = Asset.BTC },
    new RequestOptions { IdempotencyKey = "order-1042-attempt-1" });

var replayed = velirapay.LastResponse?.WasReplayed; // true when the API answered from the earlier request
```

What the API does with a key:

- A successful answer is replayed for 24 hours to a request repeating the key, with `WasReplayed` true. A request that failed stores nothing, so repeating it runs it again.
- Reuse a key only to retry the same request. A new attempt needs a new key: a charge replacing one that expired, under the key of the first, would get the expired charge back for a day. Hence keys such as `order-1042-attempt-1`, not the bare order number.
- A key covers one account, one mode, one method and one path, and every API key of that account and mode shares it. Reusing it with a different body or query string throws a `ConflictException`.
- A request holds its key for up to 60 seconds while it is processed; another one with the same key in that time gets a 409 with `Retry-After: 1`, which the library waits out and retries.
- A key is at most 255 characters of printable ASCII.

An answer that carries a secret is never stored: creating an API key (`ApiKeys.CreateAsync()`) and adding a webhook endpoint (`WebhookEndpoints.CreateAsync()`) or rotating its secret (`RotateSecretAsync()`) succeed once, and a repeat with the same key, the library's own retry after a lost answer included, throws a `ConflictException` whose `WasReplayed` is true. The write landed, but its secret is gone, so recover rather than repeat with a new key:

- **An API key:** list the keys, revoke the one you never saw, and create another.
- **A webhook endpoint:** list the endpoints and rotate the new one's secret, or delete it and add it again.
- **A rotated secret:** rotate again, and put the new secret in place straight away, since the secret your receiver holds stops signing as soon as you rotate again.

```csharp
try
{
    var apiKey = await velirapay.ApiKeys.CreateAsync(new ApiKeyCreateOptions { Name = "Shop server" }, new RequestOptions { IdempotencyKey = "shop-server-key-1" });
}
catch (ConflictException exception) when (exception.WasReplayed)
{
    // The key was created, but the answer carrying it was lost: find it with ApiKeys.ListAsync(), where its Prefix
    // and CreatedAt tell it apart, revoke it, and create another under a new idempotency key.
}
```

## Test mode

Keys starting with `vp_test_` work in test mode: charges are paid with test coins on each chain's test network, and nothing touches real funds. Test and live objects are fully separate, each mode has its own webhooks, and emails only go to members of your account. The coins of BNB Chain, Base, Arbitrum One and Polygon have no test mode, so they can only be used with a live key.

```csharp
var isTest = velirapay.IsTestMode; // read from the key's prefix

// Pay a test charge without any coins: it becomes paid, and the webhooks go out as for a real payment.
await velirapay.Charges.SimulateAsync("k3v9x2m7q8wz");
await velirapay.Charges.SimulateAsync("k3v9x2m7q8wz", new ChargeSimulateOptions { Amount = 0.001m }); // less than is due, to try an underpayment
```

[`examples/ChargeACustomer`](https://github.com/VeliraPay/velirapay-dotnet/tree/main/examples/ChargeACustomer) creates a test charge and reads it back: `VELIRAPAY_API_KEY=vp_test_… dotnet run --project examples/ChargeACustomer`.

## Configuration

```csharp
var velirapay = new VeliraPayClient(Environment.GetEnvironmentVariable("VELIRAPAY_API_KEY"), new VeliraPayClientOptions
{
    BaseUrl = VeliraPayClient.DefaultBaseUrl,   // https://api.velirapay.com
    MaxRetries = 2,                             // 0 turns retries off
    MaxRetryAfter = TimeSpan.FromSeconds(10),   // the longest Retry-After waited out before trying again
    Timeout = TimeSpan.FromSeconds(30),         // how long one attempt may take
    AppInfo = "MyShop/2.1",                     // added to the User-Agent
});
```

A client is safe to share between threads; keep one for the life of your application. Without an `HttpClient` of your own, every client shares one the library makes, which does not follow redirects and renews its connections every few minutes.

To use `IHttpClientFactory`, give the client the `HttpClient` it makes. Registered as a typed client, it needs no package beyond `Microsoft.Extensions.Http`, which ASP.NET Core already has:

```csharp
var builder = WebApplication.CreateBuilder(args);

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
