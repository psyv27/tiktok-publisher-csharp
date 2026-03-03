namespace TikTokPublisher.Models;

public class TokenResponse
{
    /// <summary>
    /// Access token for API calls
    /// </summary>
    public string? access_token { get; set; }

    /// <summary>
    /// Token type (e.g., "Bearer")
    /// </summary>
    public string? token_type { get; set; }

    /// <summary>
    /// Access token expiration in seconds
    /// </summary>
    public int expires_in { get; set; }

    /// <summary>
    /// Refresh token for obtaining new access tokens
    /// </summary>
    public string? refresh_token { get; set; }

    /// <summary>
    /// Refresh token expiration in seconds
    /// </summary>
    public int refresh_expires_in { get; set; }

    /// <summary>
    /// Granted scopes
    /// </summary>
    public string? scope { get; set; }

    /// <summary>
    /// TikTok user Open ID
    /// </summary>
    public string? open_id { get; set; }

    /// <summary>
    /// Error description (if any)
    /// </summary>
    public string? error_description { get; set; }

    /// <summary>
    /// Error code (if any)
    /// </summary>
    public string? error { get; set; }

    /// <summary>
    /// Get Credentials from token response
    /// </summary>
    public Models.Credentials? ToCredentials(string clientId, string clientSecret, string redirectUri)
    {
        if (string.IsNullOrEmpty(access_token) || string.IsNullOrEmpty(refresh_token))
        {
            return null;
        }

        return new Models.Credentials
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            RedirectUri = redirectUri,
            AccessToken = access_token,
            RefreshToken = refresh_token,
            OpenId = open_id ?? string.Empty,
            ExpiresIn = expires_in,
            RefreshExpiresIn = refresh_expires_in,
            Scopes = scope ?? string.Empty,
            Status = "configured"
        };
    }

    /// <summary>
    /// Check if response contains error
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(error);
}
