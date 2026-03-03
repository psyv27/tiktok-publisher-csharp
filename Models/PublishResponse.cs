namespace TikTokPublisher.Models;

public class PublishResponse
{
    /// <summary>
    /// Response data
    /// </summary>
    public PublishData? data { get; set; }

    /// <summary>
    /// Error information
    /// </summary>
    public PublishError? error { get; set; }
}

public class PublishData
{
    /// <summary>
    /// Publish ID for tracking upload status
    /// </summary>
    public string? publish_id { get; set; }

    /// <summary>
    /// Upload URL for video file
    /// </summary>
    public string? upload_url { get; set; }

    /// <summary>
    /// Request log ID for debugging
    /// </summary>
    public string? log_id { get; set; }
}

public class PublishError
{
    /// <summary>
    /// Error code
    /// </summary>
    public string? code { get; set; }

    /// <summary>
    /// Error message description
    /// </summary>
    public string? message { get; set; }

    /// <summary>
    /// Request log ID
    /// </summary>
    public string? log_id { get; set; }
}

public class StatusResponse
{
    public StatusData? data { get; set; }
    public PublishError? error { get; set; }
}

public class StatusData
{
    public string? publish_id { get; set; }
    public string? status { get; set; }
    public string? status_name { get; set; }
    public object? data { get; set; }
    public string? log_id { get; set; }
}

public class CreatorInfoResponse
{
    public CreatorInfoData? data { get; set; }
    public PublishError? error { get; set; }
}

public class CreatorInfoData
{
    public List<string>? privacy_level_options { get; set; }
    public string? creator_nickname { get; set; }
    public string? creator_username { get; set; }
    public string? log_id { get; set; }
}

/// <summary>
/// Upload result summary
/// </summary>
public class UploadResult
{
    public required string VideoPath { get; set; }
    public required string PublishId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public static UploadResult Failed(string videoPath, string errorMessage)
    {
        return new UploadResult
        {
            VideoPath = videoPath,
            PublishId = string.Empty,
            Success = false,
            ErrorMessage = errorMessage
        };
    }

    public static UploadResult Succeeded(string videoPath, string publishId)
    {
        return new UploadResult
        {
            VideoPath = videoPath,
            PublishId = publishId,
            Success = true
        };
    }
}
