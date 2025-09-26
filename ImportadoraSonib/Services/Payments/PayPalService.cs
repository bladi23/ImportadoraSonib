using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ImportadoraSonib.Services;

public class PayPalService
{
    private readonly HttpClient _http;
    private readonly string _clientId;
    private readonly string _secret;
    private readonly string _base;

    public PayPalService(HttpClient http, IConfiguration cfg)
    {
        _http = http;
        _clientId = cfg["PayPal:ClientId"]!;
        _secret   = cfg["PayPal:Secret"]!;
        _base     = cfg["PayPal:BaseUrl"] ?? "https://api-m.sandbox.paypal.com";
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"{_base}/v1/oauth2/token");
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_secret}"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        req.Content = new FormUrlEncodedContent(new Dictionary<string,string> { ["grant_type"] = "client_credentials" });

        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();
        using var s = await res.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(s);
        return doc.RootElement.GetProperty("access_token").GetString()!;
    }

    public async Task<(string paypalOrderId, string approveUrl)> CreateOrderAsync(decimal amount, string currency, string returnUrl, string cancelUrl)
    {
        var token = await GetAccessTokenAsync();

        var body = new
        {
            intent = "CAPTURE",
            purchase_units = new[] {
                new {
                    amount = new { currency_code = currency, value = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) }
                }
            },
            application_context = new {
                return_url = returnUrl,
                cancel_url = cancelUrl,
                user_action = "PAY_NOW"
            }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, $"{_base}/v2/checkout/orders");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();

        using var s = await res.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(s);

        var id = doc.RootElement.GetProperty("id").GetString()!;
        var links = doc.RootElement.GetProperty("links").EnumerateArray();
        var approve = links.First(x => x.GetProperty("rel").GetString() == "approve").GetProperty("href").GetString()!;
        return (id, approve);
    }

    public async Task CaptureOrderAsync(string paypalOrderId)
    {
        var token = await GetAccessTokenAsync();
        var req = new HttpRequestMessage(HttpMethod.Post, $"{_base}/v2/checkout/orders/{paypalOrderId}/capture");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode(); // lanza si falla
    }
}
