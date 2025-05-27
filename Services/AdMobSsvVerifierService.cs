using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Api.Services
{
    public interface IAdMobSsvVerifierService
    {
        Task<bool> VerifyCallbackAsync(string? rawQueryString, string? signature, string? keyIdStr);
    }

    public class AdMobSsvVerifierService : IAdMobSsvVerifierService
    {
        private readonly IAdMobPublicKeyService _publicKeyService;
        private readonly ILogger<AdMobSsvVerifierService> _logger;

        public AdMobSsvVerifierService(IAdMobPublicKeyService publicKeyService, ILogger<AdMobSsvVerifierService> logger)
        {
            _publicKeyService = publicKeyService;
            _logger = logger;
        }

        public async Task<bool> VerifyCallbackAsync(string? rawQueryString, string? signatureBase64Url, string? keyIdStr)
        {
            if (string.IsNullOrEmpty(rawQueryString) || string.IsNullOrEmpty(signatureBase64Url) || string.IsNullOrEmpty(keyIdStr))
            {
                _logger.LogWarning("SSV verification failed: Missing rawQueryString, signature, or keyId.");
                return false;
            }

            if (!long.TryParse(keyIdStr, out long keyId))
            {
                _logger.LogWarning("SSV verification failed: Could not parse keyId: {KeyIdStr}", keyIdStr);
                return false;
            }

            string? publicKeyBase64 = await _publicKeyService.GetPublicKeyBase64Async(keyId);
            if (string.IsNullOrEmpty(publicKeyBase64))
            {
                _logger.LogWarning("SSV verification failed: Public key not found for keyId: {KeyId}", keyId);
                return false;
            }

            try
            {
                // Construct the content to verify: all query params except signature and key_id, in alphabetical order.
                var queryParams = HttpUtility.ParseQueryString(rawQueryString);
                string contentToVerify = string.Join("&", queryParams.AllKeys
                    .Where(k => k != null && k != "signature" && k != "key_id") // Ensure k is not null
                    .OrderBy(k => k) // Alphabetical order
                    .Select(k => $"{k}={queryParams[k]}"));

                var contentBytes = Encoding.UTF8.GetBytes(contentToVerify);
                
                // The signature from AdMob is Base64 URL safe. We need to convert it to standard Base64 before decoding.
                string signatureBase64 = signatureBase64Url.Replace('-', '+').Replace('_', '/');
                // Add padding if necessary
                switch (signatureBase64.Length % 4)
                {
                    case 2: signatureBase64 += "=="; break;
                    case 3: signatureBase64 += "="; break;
                }
                var signatureBytes = Convert.FromBase64String(signatureBase64);

                using (ECDsa ecdsa = ECDsa.Create())
                {
                    // AdMob public keys are Base64 encoded (not PEM in the JSON structure we parse for Base64 field).
                    // We need to import it as a Pkcs8 public key blob, which is what FromBase64String on a raw public key gives us.
                    // However, standard .NET ECDsa needs specific formats. It might be easier to construct from ECParameters if we know the curve.
                    // AdMob uses ECDSA with NIST P-256 curve (secp256r1) and SHA256 hashing.
                    // The public keys from AdMob are typically ASN.1 encoded SubjectPublicKeyInfo (X.509 format).
                    
                    // Simplest way if key is raw X,Y coordinates (unlikely from AdMob directly this way)
                    // Or, if the Base64 string is a SubjectPublicKeyInfo structure:
                    ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKeyBase64), out _);

                    // VerifyData uses DER signature format by default which AdMob uses.
                    // DSASignatureFormat.Rfc3279DerSequence is the default for VerifyData.
                    bool isValid = ecdsa.VerifyData(contentBytes, signatureBytes, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
                    
                    _logger.LogInformation("SSV Verification for keyId {KeyId}. Content: '{ContentToVerify}'. Signature Valid: {IsValid}", keyId, contentToVerify, isValid);
                    return isValid;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SSV signature verification for keyId {KeyId}. Query: {Query}", keyId, rawQueryString);
                return false;
            }
        }
    }
} 