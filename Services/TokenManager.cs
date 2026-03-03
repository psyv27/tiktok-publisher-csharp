using System.Text.Json;
using TikTokPublisher.Models;

namespace TikTokPublisher.Services;

/// <summary>
/// Token manager - loads, saves, and refreshes tokens
/// </summary>
public class TokenManager
{
    private readonly string _credentialsPath;
    private Credentials? _credentials;
    private DateTime _tokenIssuedAt = DateTime.UtcNow;

    public TokenManager(string credentialsPath = "Config/api_credentials.json")
    {
        _credentialsPath = credentialsPath;
    }

    /// <summary>
    /// Load credentials from file
    /// </summary>
    public bool LoadCredentials()
    {
        if (!File.Exists(_credentialsPath))
        {
            Console.WriteLine($"❌ Credentials file not found: {_credentialsPath}");
            Console.WriteLine("   Please create api_credentials.json from the example template");
            return false;
        }

        try
        {
            var jsonContent = File.ReadAllText(_credentialsPath);
            _credentials = JsonSerializer.Deserialize<Credentials>(jsonContent);

            if (_credentials == null || !_credentials.IsValid())
            {
                Console.WriteLine("❌ Invalid credentials or not configured");
                return false;
            }

            Console.WriteLine("✅ Credentials loaded successfully");
            Console.WriteLine($"   Client ID: {_credentials.ClientId}");
            Console.WriteLine($"   Redirect URI: {_credentials.RedirectUri}");
            Console.WriteLine($"   Access Token: {_credentials.AccessToken[0..30]}...");
            Console.WriteLine($"   Open ID: {_credentials.OpenId}");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to load credentials: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Save credentials to file
    /// </summary>
    public bool SaveCredentials(Credentials credentials)
    {
        try
        {
            var directory = Path.GetDirectoryName(_credentialsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var jsonContent = JsonSerializer.Serialize(credentials, options);
            File.WriteAllText(_credentialsPath, jsonContent);

            _credentials = credentials;
            _tokenIssuedAt = DateTime.UtcNow;

            Console.WriteLine($"✅ Credentials saved to: {_credentialsPath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to save credentials: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Update credentials with new token response
    /// </summary>
    public bool UpdateTokens(TokenResponse tokenResponse)
    {
        if (tokenResponse != null && tokenResponse.HasError)
        {
            return false;
        }

        var updatedCredentials = new Credentials
        {
            ClientId = _credentials?.ClientId ?? string.Empty,
            ClientSecret = _credentials?.ClientSecret ?? string.Empty,
            RedirectUri = _credentials?.RedirectUri ?? string.Empty,
            AccessToken = tokenResponse?.access_token ?? string.Empty,
            RefreshToken = tokenResponse?.refresh_token ?? string.Empty,
            OpenId = tokenResponse?.open_id ?? string.Empty,
            ExpiresIn = tokenResponse?.expires_in ?? 0,
            RefreshExpiresIn = tokenResponse?.refresh_expires_in ?? 0,
            Scopes = tokenResponse?.scope ?? string.Empty,
            Status = "configured"
        };

        return SaveCredentials(updatedCredentials);
    }

    /// <summary>
    /// Get credentials
    /// </summary>
    public Credentials? GetCredentials()
    {
        return _credentials;
    }

    /// <summary>
    /// Check if access token needs refresh
    /// </summary>
    public bool ShouldRefreshAccessToken()
    {
        if (_credentials == null)
        {
            return false;
        }

        return _credentials.IsAccessTokenExpired(_tokenIssuedAt);
    }

    /// <summary>
    /// Refresh access token automatically
    /// </summary>
    public async Task<bool> RefreshAccessTokenAsync(TikTokApiClient apiClient)
    {
        Console.WriteLine("🔄 Refreshing access token...");

        var tokenResponse = await apiClient.RefreshAccessTokenAsync();

        if (tokenResponse == null || tokenResponse.HasError)
        {
            Console.WriteLine("❌ Failed to refresh access token");
            Console.WriteLine("   Please re-run authentication flow or check refresh token");
            return false;
        }

        var success = UpdateTokens(tokenResponse);

        if (success)
        {
            Console.WriteLine("✅ Access token refreshed successfully");
            Console.WriteLine($"   New Access Token: {tokenResponse.access_token?[0..30]}...");
        }

        return success;
    }

    /// <summary>
    /// Generate authorization URL
    /// </summary>
    public string GenerateAuthorizationUrl(string scope = "video.upload,user.info.basic", string state = "123")
    {
        var baseUrl = "https://www.tiktok.com/v2/auth/authorize/";
        var redirectUriEncoded = Uri.EscapeDataString(_credentials?.RedirectUri ?? string.Empty);
        var scopeEncoded = Uri.EscapeDataString(scope);

        var url = $"{baseUrl}?" +
                  $"client_key={_credentials?.ClientId}&" +
                  $"redirect_uri={redirectUriEncoded}&" +
                  $"response_type=code&" +
                  $"scope={scopeEncoded}&" +
                  $"state={state}";

        return url;
    }

    /// <summary>
    /// Extract authorization code from callback URL
    /// </summary>
    public static string? ExtractAuthorizationCode(string callbackUrl)
    {
        try
        {
            // Manual parsing for callback URL
            // Format: https://oauth.pstmn.io/v1/callback?code=xxx&...

            if (!callbackUrl.Contains("code="))
            {
                return null;
            }

            var startIndex = callbackUrl.IndexOf("code=") + 5;

            // Find next parameter or end of string
            var endIndex = callbackUrl.IndexOf("&", startIndex);
            if (endIndex == -1 || endIndex < startIndex)
            {
                endIndex = callbackUrl.Length;
            }

            return callbackUrl.Substring(startIndex, endIndex - startIndex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to extract authorization code: {ex.Message}");
            return null;
        }
    }
}
