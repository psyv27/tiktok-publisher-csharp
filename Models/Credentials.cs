namespace TikTokPublisher.Models;

public class Credentials
{
    /// <summary>
    /// TikTok Client ID from Developer Console
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// TikTok Client Secret from Developer Console
    /// </summary>
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>
    /// OAuth redirect URI (Postman callback by default)
    /// </summary>
    public string RedirectUri { get; init; } = "https://oauth.pstmn.io/v1/callback";

    /// <summary>
    /// Access token for API calls
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token for obtaining new access tokens
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// TikTok user Open ID
    /// </summary>
    public string OpenId { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration in seconds
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Refresh token expiration in seconds
    /// </summary>
    public int RefreshExpiresIn { get; set; }

    /// <summary>
    /// Granted scopes
    /// </summary>
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    /// Configuration status
    /// </summary>
    public string Status { get; set; } = "not_configured";

    /// <summary>
    /// Check if credentials are valid
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(ClientId)
               && !string.IsNullOrEmpty(ClientSecret)
               && !string.IsNullOrEmpty(AccessToken)
               && !string.IsNullOrEmpty(RefreshToken)
               && !string.IsNullOrEmpty(OpenId);
    }

    /// <summary>
    /// Check if access token is expired
    /// </summary>
    public bool IsAccessTokenExpired(DateTime tokenIssuedAt)
    {
        // Add 5 minute buffer before actual expiration
        var bufferMinutes = 5;
        var expirationTime = tokenIssuedAt.AddSeconds(ExpiresIn).AddMinutes(-bufferMinutes);
        return DateTime.UtcNow > expirationTime;
    }
}
