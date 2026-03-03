using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TikTokPublisher.Models;

namespace TikTokPublisher.Services;

/// <summary>
/// TikTok Content Publishing API v2 Client
/// </summary>
public class TikTokApiClient
{
    private readonly HttpClient _httpClient;
    private Credentials _credentials;

    // API Endpoints
    private const string BaseUrl = "https://open.tiktokapis.com/v2";
    private const string AuthUrl = "https://open.tiktokapis.com/v2/oauth/token/";

    // Content Posting Endpoints
    private const string InboxInitUrl = "/v2/post/publish/inbox/video/init/";
    private const string PublishInitUrl = "/v2/post/publish/video/init/";
    private const string PublishStatusUrl = "/v2/post/publish/status/fetch/";
    private const string CreatorInfoUrl = "/v2/post/publish/creator_info/query/";

    public TikTokApiClient(HttpClient httpClient, Credentials credentials)
    {
        _httpClient = httpClient;
        _credentials = credentials;
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Exchange authorization code for access token
    /// </summary>
    public async Task<TokenResponse?> ExchangeCodeForTokenAsync(string authorizationCode)
    {
        var requestBody = new Dictionary<string, string>
        {
            { "client_key", _credentials.ClientId },
            { "client_secret", _credentials.ClientSecret },
            { "code", authorizationCode },
            { "grant_type", "authorization_code" },
            { "redirect_uri", _credentials.RedirectUri }
        };

        var content = new FormUrlEncodedContent(requestBody);

        try
        {
            var response = await _httpClient.PostAsync(AuthUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

            if (tokenResponse != null && tokenResponse.HasError)
            {
                Console.WriteLine($"Token exchange error: {tokenResponse.error_description}");
                return null;
            }

            Console.WriteLine("✅ Token exchanged successfully!");
            Console.WriteLine($"   Access Token: {tokenResponse?.access_token?[0..30]}...");
            Console.WriteLine($"   Refresh Token: {tokenResponse?.refresh_token?[0..30]}...");
            Console.WriteLine($"   Open ID: {tokenResponse?.open_id}");
            Console.WriteLine($"   Expires in: {tokenResponse?.expires_in} seconds");

            return tokenResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Token exchange failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    public async Task<TokenResponse?> RefreshAccessTokenAsync()
    {
        var requestBody = new Dictionary<string, string>
        {
            { "client_key", _credentials.ClientId },
            { "client_secret", _credentials.ClientSecret },
            { "grant_type", "refresh_token" },
            { "refresh_token", _credentials.RefreshToken }
        };

        var content = new FormUrlEncodedContent(requestBody);

        try
        {
            var response = await _httpClient.PostAsync(AuthUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

            if (tokenResponse != null && tokenResponse.HasError)
            {
                Console.WriteLine($"Token refresh error: {tokenResponse.error_description}");
                return null;
            }

            Console.WriteLine("✅ Token refreshed successfully!");
            return tokenResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Token refresh failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Query creator information
    /// </summary>
    public async Task<CreatorInfoResponse?> GetCreatorInfoAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, CreatorInfoUrl);

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _credentials.AccessToken);

        var body = new { };
        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        try
        {
            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<CreatorInfoResponse>(responseContent);

            if (result != null && result.error != null)
            {
                Console.WriteLine($"Creator info error: {result.error.message}");
                return null;
            }

            Console.WriteLine("✅ Creator info retrieved:");
            Console.WriteLine($"   Nickname: {result?.data?.creator_nickname}");
            Console.WriteLine($"   Username: {result?.data?.creator_username}");
            Console.WriteLine($"   Privacy options: {string.Join(", ", result?.data?.privacy_level_options ?? new List<string>())}");
            Console.WriteLine($"   Log ID: {result?.data?.log_id}");

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Get creator info failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Initialize upload as draft
    /// </summary>
    public async Task<PublishResponse?> InitializeDraftUploadAsync(string filePath, long fileSize)
    {
        var request = new VideoUploadRequest.CreateDraftRequest(filePath, fileSize);

        return await InitializeUploadAsync(request);
    }

    /// <summary>
    /// Initialize upload as draft
    /// </summary>
    public async Task<PublishResponse?> InitializeUploadAsync(VideoUploadRequest uploadRequest)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, InboxInitUrl);

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _credentials.AccessToken);

        var body = new
        {
            video_info = uploadRequest.video_info,
            post_info = uploadRequest.post_info
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        try
        {
            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<PublishResponse>(responseContent);

            Console.WriteLine($"Initialize upload response (Status: {response.StatusCode}):");
            Console.WriteLine(responseContent);

            if (result != null && result.error != null)
            {
                Console.WriteLine($"❌ Upload init error: {result.error.message}");
                Console.WriteLine($"   Error code: {result.error.code}");
                Console.WriteLine($"   Log ID: {result.error.log_id}");
                return null;
            }

            Console.WriteLine("✅ Upload initialized successfully:");
            Console.WriteLine($"   Publish ID: {result?.data?.publish_id}");
            Console.WriteLine($"   Upload URL: {result?.data?.upload_url?[0..50]}...");
            Console.WriteLine($"   Log ID: {result?.data?.log_id}");

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Initialize upload failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Upload video file to CDN
    /// </summary>
    public async Task<bool> UploadVideoFileAsync(string uploadUrl, string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"❌ Video file not found: {filePath}");
            return false;
        }

        var fileInfo = new FileInfo(filePath);
        var fileSize = fileInfo.Length;

        Console.WriteLine($"📤 Uploading video file...");
        Console.WriteLine($"   File: {Path.GetFileName(filePath)}");
        Console.WriteLine($"   Size: {FormatFileSize(fileSize)}");
        Console.WriteLine($"   URL: {uploadUrl[0..50]}...");

        var requestBody = File.ReadAllBytes(filePath);

        // Create new HTTP client for CDN upload (without base address)
        using var cdnClient = new HttpClient();
        cdnClient.Timeout = TimeSpan.FromMinutes(10); // 10 minute timeout

        using var content = new StreamContent(new MemoryStream(requestBody));
        content.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
        content.Headers.ContentRange = new ContentRangeHeaderValue(0, fileSize - 1, fileSize);

        try
        {
            var response = await cdnClient.PutAsync(uploadUrl, content);

            Console.WriteLine($"CDN Upload Status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ CDN upload failed: {response.StatusCode}");
                Console.WriteLine($"   Response: {errorContent}");
                return false;
            }

            Console.WriteLine("✅ Video file uploaded to CDN successfully!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ CDN upload failed: {ex.Message}");
            Console.WriteLine($"   {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Check publish status
    /// </summary>
    public async Task<StatusResponse?> CheckPublishStatusAsync(string publishId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, PublishStatusUrl);

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _credentials.AccessToken);

        var body = new { publish_id = publishId };
        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        try
        {
            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<StatusResponse>(responseContent);

            Console.WriteLine($"Publish status: {result?.data?.status_name} ({result?.data?.status})");

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Check status failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Format file size in human readable format
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Update credentials (for token refresh)
    /// </summary>
    public void UpdateCredentials(Credentials? credentials)
    {
        if (credentials != null)
        {
            _credentials = credentials;
            Console.WriteLine("✅ API client credentials updated");
        }
    }
}
