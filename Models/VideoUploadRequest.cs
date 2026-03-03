namespace TikTokPublisher.Models;

/// <summary>
/// Video upload request for TikTok API
/// </summary>
public class VideoUploadRequest
{
    /// <summary>
    /// File info (JSON stringified)
    /// </summary>
    public string? video_info { get; set; }

    /// <summary>
    /// Post info (JSON stringified)
    /// </summary>
    public string? post_info { get; set; }

    /// <summary>
    /// Create draft upload request (POST to inbox)
    /// </summary>
    public static VideoUploadRequest CreateDraftRequest(
        string filePath,
        long fileSize)
    {
        var fileInfo = new VideoInfo
        {
            file_size = fileSize
        };

        return new VideoUploadRequest
        {
            video_info = System.Text.Json.JsonSerializer.Serialize(fileInfo)
        };
    }

    /// <summary>
    /// Create direct publish request
    /// </summary>
    public static VideoUploadRequest CreatePublishRequest(
        string filePath,
        long fileSize,
        string caption,
        string PrivacyLevel = "MUTUAL_FOLLOW_FRIENDS")
    {
        var fileInfo = new VideoInfo
        {
            file_size = fileSize
        };

        var postInfo = new PostInfo
        {
            title = caption,
            privacy_level = PrivacyLevel,
            disable_duet = false,
            disable_stitch = false,
            disable_comment = false
        };

        return new VideoUploadRequest
        {
            video_info = System.Text.Json.JsonSerializer.Serialize(fileInfo),
            post_info = System.Text.Json.JsonSerializer.Serialize(postInfo)
        };
    }
}

/// <summary>
/// Video file info
/// </summary>
public class VideoInfo
{
    public long file_size { get; set; }
}

/// <summary>
/// Post information for direct publishing
/// </summary>
public class PostInfo
{
    public string title { get; set; } = string.Empty;
    public string privacy_level { get; set; } = "MUTUAL_FOLLOW_FRIENDS";
    public bool disable_duet { get; set; } = false;
    public bool disable_stitch { get; set; } = false;
    public bool disable_comment { get; set; } = false;

    // Optional fields
    public List<string>? hashtags { get; set; }
    public string? location_id { get; set; }
}
