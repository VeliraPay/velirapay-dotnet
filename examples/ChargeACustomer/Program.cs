// Creates a test-mode charge and reads it back:
//     VELIRAPAY_API_KEY=vp_test_… dotnet run --project examples/ChargeACustomer
using System.Globalization;
using VeliraPay;

var apiKey = Environment.GetEnvironmentVariable("VELIRAPAY_API_KEY");

if (apiKey is null || !apiKey.StartsWith("vp_test_", StringComparison.Ordinal))
{
    Console.Error.WriteLine("This example only runs with a test-mode key, starting with vp_test_, in VELIRAPAY_API_KEY.");

    return 1;
}

var velirapay = new VeliraPayClient(apiKey, new VeliraPayClientOptions { AppInfo = "velirapay-dotnet-example/0.2.0" });

var charge = await velirapay.Charges.CreateAsync(new ChargeCreateOptions
{
    Amount = 49.90m,
    Currency = Currency.EUR,
    Asset = Asset.BTC,
    Customer = new CustomerOptions { Email = "ada@example.com", Name = "Ada Lovelace" },
    Description = "Example charge from velirapay-dotnet",
    Metadata = new() { ["order_id"] = "example-1" },
});

var retrieved = await velirapay.Charges.RetrieveAsync(charge.Id);

Console.WriteLine($"Charge:          {retrieved.Id} ({retrieved.Status})");
Console.WriteLine($"Amount:          {retrieved.FiatAmount.ToString(CultureInfo.InvariantCulture)} {retrieved.FiatCurrency}");
Console.WriteLine($"To send:         {retrieved.AssetAmount.ToString(CultureInfo.InvariantCulture)} {retrieved.Asset}");
Console.WriteLine($"Deposit address: {retrieved.DepositAddress}");
Console.WriteLine($"Checkout:        {retrieved.CheckoutUrl}");
Console.WriteLine($"Expires at:      {retrieved.ExpiresAt:u}");

return 0;
