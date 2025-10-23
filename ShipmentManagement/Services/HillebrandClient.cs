using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ShipmentManagement.Services
{
    public class HillebrandClient : IHillebrandClient
    {
        private readonly HttpClient _http;
        private readonly TokenCache _tokenCache;
        private readonly IConfiguration _config;
        private readonly ILogger<HillebrandClient> _logger;

        public HillebrandClient(HttpClient http, TokenCache tokenCache, IConfiguration config, ILogger<HillebrandClient> logger)
        {
            _http = http;
            _tokenCache = tokenCache;
            _config = config;
            _logger = logger;
        }

        private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            // Return cached token if still valid
            if (!string.IsNullOrEmpty(_tokenCache.AccessToken) &&
                _tokenCache.Expiry.HasValue &&
                _tokenCache.Expiry.Value > DateTimeOffset.UtcNow.AddSeconds(30))
            {
                return _tokenCache.AccessToken!;
            }

            try
            {
                var section = _config.GetSection("Hillebrand");
                var tokenUrl = section["HGB_TOKEN_URL"]
                               ?? throw new InvalidOperationException("HGB_TOKEN_URL not set");
                var clientId = section["HGB_CLIENT_ID"];
                var clientSecret = section["HGB_CLIENT_SECRET"];
                var username = section["HGB_USERNAME"];
                var password = section["HGB_PASSWORD"];

                // Form content for OAuth password grant
                var form = new Dictionary<string, string>
                {
                    ["grant_type"] = "password",
                    ["username"] = username ?? string.Empty,
                    ["password"] = password ?? string.Empty,
                    ["scope"] = "offline_access"
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
                {
                    Content = new FormUrlEncodedContent(form)
                };
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Set Basic Authorization header
                if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
                {
                    var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
                    req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
                }

                // Log form preview (redacted password)
                var preview = form.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Key.Equals("password", StringComparison.OrdinalIgnoreCase) ? "***REDACTED***" : kvp.Value
                );
                _logger.LogDebug("Token request to {Url} preview: {Preview}", tokenUrl, preview);

                using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseContentRead, cancellationToken);
                var txt = await resp.Content.ReadAsStringAsync(cancellationToken);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get token: {Status} {Body}", resp.StatusCode, txt);
                    throw new InvalidOperationException($"Failed to obtain access token from Hillebrand: {txt}");
                }

                var json = JsonDocument.Parse(txt).RootElement;
                if (!json.TryGetProperty("access_token", out var tokenProp))
                    throw new InvalidOperationException("Invalid token response");

                var accessToken = tokenProp.GetString();
                var expiresIn = json.TryGetProperty("expires_in", out var ex) && ex.ValueKind == JsonValueKind.Number
                    ? ex.GetInt32()
                    : 3600;

                _tokenCache.AccessToken = accessToken;
                _tokenCache.Expiry = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

                return accessToken!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obtaining access token");
                throw;
            }
        }

        public async Task<JsonElement> GetShipmentsAsync(string? mainModality, int pageNumber, int pageSize)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();

                var baseUrl = _config.GetSection("Hillebrand")["HGB_BASE_URL"]
                              ?? "https://api.hillebrandgori.com";
                var builder = new StringBuilder($"{baseUrl.TrimEnd('/')}/v6/shipments?");
                builder.Append($"pageNumber={pageNumber}&pageSize={pageSize}");
                if (!string.IsNullOrEmpty(mainModality))
                    builder.Append($"&mainModality={Uri.EscapeDataString(mainModality)}");

                var url = builder.ToString();

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseContentRead);
                var content = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogError("Hillebrand GET {Url} returned {Status}: {Body}", url, res.StatusCode, content);
                    throw new InvalidOperationException("Hillebrand API error");
                }

                return JsonDocument.Parse(content).RootElement;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching shipments");
                throw;
            }
        }

        public async Task<JsonElement> GetShipmentByIdAsync(string id)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                var baseUrl = _config.GetSection("Hillebrand")["HGB_BASE_URL"]
                              ?? "https://api.hillebrandgori.com";
                var url = $"{baseUrl.TrimEnd('/')}/v6/shipments/{Uri.EscapeDataString(id)}";

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseContentRead);
                var content = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogError("Hillebrand GET {Url} returned {Status}: {Body}", url, res.StatusCode, content);
                    throw new InvalidOperationException("Hillebrand API error");
                }

                return JsonDocument.Parse(content).RootElement;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching shipment by id: {Id}", id);
                throw;
            }
        }
    }
}
