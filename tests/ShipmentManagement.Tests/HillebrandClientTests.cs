using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using ShipmentManagement.Services;
using Xunit;

namespace ShipmentManagement.Tests
{
    public class HillebrandClientTests
    {
        private const string BaseUrl = "https://api.hillebrandgori.com";

        private HillebrandClient CreateClient(HttpStatusCode statusCode, string responseBody = "{}")
        {
            var handler = new Mock<HttpMessageHandler>();

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync((HttpRequestMessage req, CancellationToken token) =>
                {
                    if (req.RequestUri!.AbsoluteUri.Contains("/token"))
                    {
                        // Token endpoint always returns valid fake token
                        var tokenJson = JsonSerializer.Serialize(new
                        {
                            access_token = "fake-token",
                            expires_in = 3600
                        });
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(tokenJson, Encoding.UTF8, "application/json")
                        };
                    }

                    if (req.RequestUri.AbsoluteUri.Contains("/v6/shipments"))
                    {
                        // Shipments endpoint returns whatever status/response we want
                        return new HttpResponseMessage(statusCode)
                        {
                            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                        };
                    }

                    return new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent("{\"message\":\"not found\"}", Encoding.UTF8, "application/json")
                    };
                });

            var httpClient = new HttpClient(handler.Object)
            {
                BaseAddress = new Uri(BaseUrl)
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("Hillebrand:HGB_BASE_URL", BaseUrl),
                    new KeyValuePair<string, string>("Hillebrand:HGB_TOKEN_URL", "https://login.hillebrand.com/oauth2/tenant/v1/token"),
                    new KeyValuePair<string, string>("Hillebrand:HGB_CLIENT_ID", "id"),
                    new KeyValuePair<string, string>("Hillebrand:HGB_CLIENT_SECRET", "secret"),
                    new KeyValuePair<string, string>("Hillebrand:HGB_GRANT_TYPE", "password"),
                    new KeyValuePair<string, string>("Hillebrand:HGB_USERNAME", "user"),
                    new KeyValuePair<string, string>("Hillebrand:HGB_PASSWORD", "pw"),
                    new KeyValuePair<string, string>("Hillebrand:HGB_SCOPE", "openid"),
                })
                .Build();

            var tokenCache = new TokenCache();

            return new HillebrandClient(httpClient, tokenCache, config, NullLogger<HillebrandClient>.Instance);
        }

        [Fact]
        public async Task GetShipmentsAsync_ReturnsJson_WhenStatusIs200()
        {
            var expectedJson = "{\"result\":\"success\"}";
            var client = CreateClient(HttpStatusCode.OK, expectedJson);

            var result = await client.GetShipmentsAsync("SEA", 1, 10);

            Assert.Equal(JsonDocument.Parse(expectedJson).RootElement.GetRawText(), result.GetRawText());
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task GetShipmentsAsync_ThrowsInvalidOperationException_WhenStatusIsError(HttpStatusCode statusCode)
        {
            var client = CreateClient(statusCode);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.GetShipmentsAsync("SEA", 1, 10));

            Assert.Equal("Hillebrand API error", ex.Message);
        }

        [Fact]
        public async Task GetShipmentByIdAsync_ReturnsJson_WhenStatusIs200()
        {
            var expectedJson = "{\"id\":\"12345\"}";
            var client = CreateClient(HttpStatusCode.OK, expectedJson);

            var result = await client.GetShipmentByIdAsync("12345");

            Assert.Equal(JsonDocument.Parse(expectedJson).RootElement.GetRawText(), result.GetRawText());
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task GetShipmentByIdAsync_ThrowsInvalidOperationException_WhenStatusIsError(HttpStatusCode statusCode)
        {
            var client = CreateClient(statusCode);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.GetShipmentByIdAsync("12345"));

            Assert.Equal("Hillebrand API error", ex.Message);
        }
    }
}
