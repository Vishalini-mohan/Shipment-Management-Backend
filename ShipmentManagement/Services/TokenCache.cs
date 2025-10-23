namespace ShipmentManagement.Services
{
    // Simple token cache; production should use a more robust cache like MemoryCache or Redis
    public class TokenCache
    {
        public string? AccessToken { get; set; }
        public DateTimeOffset? Expiry { get; set; }

        public bool IsValid() =>
            !string.IsNullOrEmpty(AccessToken) && Expiry.HasValue && Expiry.Value > DateTimeOffset.UtcNow.AddSeconds(30);
    }
}
