using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Api.Services
{
    // DTO to represent the structure of a single key from AdMob
    public class AdMobPublicKey
    {
        public long KeyId { get; set; }
        public string Pem { get; set; } = string.Empty; // PEM format
        public string Base64 { get; set; } = string.Empty; // Base64 format
    }

    // DTO for the overall JSON structure from AdMob's key server
    public class AdMobPublicKeysRoot
    {
        public List<AdMobPublicKey> Keys { get; set; } = new List<AdMobPublicKey>();
    }

    public interface IAdMobPublicKeyService
    {
        Task<string?> GetPublicKeyBase64Async(long keyId);
        Task ForceRefreshKeysAsync();
    }

    public class AdMobPublicKeyService : IAdMobPublicKeyService
    {
        private const string AdMobPublicKeysUrl = "https://gstatic.com/admob/reward/verifier-keys.json";
        private const string CacheKey = "AdMobPublicKeys";
        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AdMobPublicKeyService> _logger;
        private static readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);

        public AdMobPublicKeyService(IMemoryCache cache, IHttpClientFactory httpClientFactory, ILogger<AdMobPublicKeyService> logger)
        {
            _cache = cache;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private async Task<Dictionary<long, string>> FetchAndCacheKeysAsync()
        {
            await _refreshLock.WaitAsync(); // Ensure only one thread refreshes at a time
            try
            {
                // Double-check if another thread already populated the cache while waiting for the lock
                if (_cache.TryGetValue(CacheKey, out Dictionary<long, string> cachedKeysCheck) && cachedKeysCheck != null)
                {
                    _logger.LogInformation("AdMob public keys already refreshed by another thread while waiting for lock.");
                    return cachedKeysCheck;
                }

                _logger.LogInformation("Fetching AdMob public keys from {Url}", AdMobPublicKeysUrl);
                var client = _httpClientFactory.CreateClient("AdMobKeyClient"); // Using a named client
                HttpResponseMessage response = await client.GetAsync(AdMobPublicKeysUrl);
                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var adMobKeysRoot = JsonSerializer.Deserialize<AdMobPublicKeysRoot>(jsonResponse, options);

                if (adMobKeysRoot == null || !adMobKeysRoot.Keys.Any())
                {
                    _logger.LogWarning("No AdMob public keys found or failed to deserialize.");
                    return new Dictionary<long, string>();
                }

                var newKeys = adMobKeysRoot.Keys.ToDictionary(k => k.KeyId, k => k.Base64);
                
                // Cache for 20 hours as AdMob recommends not caching > 24h and keys can rotate.
                // A shorter duration might be safer during initial testing or if rotations are frequent.
                _cache.Set(CacheKey, newKeys, TimeSpan.FromHours(20)); 
                _logger.LogInformation("Successfully fetched and cached {Count} AdMob public keys.", newKeys.Count);
                return newKeys;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching or caching AdMob public keys.");
                return new Dictionary<long, string>(); // Return empty on error to avoid breaking lookups
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        public async Task<string?> GetPublicKeyBase64Async(long keyId)
        {
            if (!_cache.TryGetValue(CacheKey, out Dictionary<long, string>? keys) || keys == null || !keys.ContainsKey(keyId))
            {
                _logger.LogInformation("AdMob public key for KeyId {KeyId} not in cache or cache expired. Refreshing...", keyId);
                keys = await FetchAndCacheKeysAsync();
            }

            if (keys.TryGetValue(keyId, out string? publicKey))
            {
                return publicKey;
            }

            _logger.LogWarning("AdMob public key for KeyId {KeyId} not found after refresh.", keyId);
            return null;
        }

        public async Task ForceRefreshKeysAsync()
        {
            _logger.LogInformation("Forcing refresh of AdMob public keys.");
            // Clear cache and fetch again
            _cache.Remove(CacheKey);
            await FetchAndCacheKeysAsync();
        }
    }
} 