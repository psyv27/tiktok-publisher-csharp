using TikTokPublisher.Models;

namespace TikTokPublisher.Services;

/// <summary>
/// Video uploader - manages video upload pipeline
/// </summary>
public class VideoUploader
{
    private readonly TikTokApiClient _apiClient;
    private readonly TokenManager _tokenManager;

    public VideoUploader(TikTokApiClient apiClient, TokenManager tokenManager)
    {
        _apiClient = apiClient;
        _tokenManager = tokenManager;
    }

    /// <summary>
    /// Upload single video as draft
    /// </summary>
    public async Task<UploadResult?> UploadSingleVideoAsync(string filePath, bool autoRefreshTokenIfNeeded = true)
    {
        Console.WriteLine($"\n{'='*60}");
        Console.WriteLine($"🎬 Uploading: {Path.GetFileName(filePath)}");
        Console.WriteLine($"{'='*60}");

        // Check if file exists
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"❌ Video file not found: {filePath}");
            return UploadResult.Failed(filePath, "File not found");
        }

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length == 0)
        {
            Console.WriteLine($"❌ Video file is empty: {filePath}");
            return UploadResult.Failed(filePath, "File is empty");
        }

        // Ensure access token is valid
        if (autoRefreshTokenIfNeeded && _tokenManager.ShouldRefreshAccessToken())
        {
            var refreshed = await _tokenManager.RefreshAccessTokenAsync(_apiClient);
            if (!refreshed)
            {
                return UploadResult.Failed(filePath, "Failed to refresh access token");
            }

            // Update API client with new token
            var credentials = _tokenManager.GetCredentials();
            _apiClient.UpdateCredentials(credentials);
        }

        // Initialize upload
        Console.WriteLine("\n1️⃣ Initializing upload...");
        var initResponse = await _apiClient.InitializeDraftUploadAsync(filePath, fileInfo.Length);

        if (initResponse == null || initResponse.error != null)
        {
            var errorMessage = initResponse?.error?.message ?? "Upload initialization failed";
            Console.WriteLine($"❌ Upload initialization failed: {errorMessage}");
            return UploadResult.Failed(filePath, errorMessage);
        }

        var uploadUrl = initResponse.data?.upload_url;
        var publishId = initResponse.data?.publish_id;

        if (string.IsNullOrEmpty(uploadUrl))
        {
            Console.WriteLine("❌ Upload URL not received");
            return UploadResult.Failed(filePath, "Upload URL missing");
        }

        // Upload file to CDN
        Console.WriteLine("\n2️⃣ Uploading to CDN...");
        var uploadSuccess = await _apiClient.UploadVideoFileAsync(uploadUrl, filePath);

        if (!uploadSuccess)
        {
            Console.WriteLine($"❌ CDN upload failed: {filePath}");
            return UploadResult.Failed(filePath, "CDN upload failed");
        }

        // Check status after upload
        Console.WriteLine("\n3️⃣ Checking upload status...");
        if (!string.IsNullOrEmpty(publishId))
        {
            await _apiClient.CheckPublishStatusAsync(publishId);
        }

        Console.WriteLine("\n✅ Upload completed successfully!");
        Console.WriteLine($"📦 Publish ID: {publishId}");
        Console.WriteLine($"📹 Video: {Path.GetFileName(filePath)}");
        Console.WriteLine($"📁 Size: {FormatFileSize(fileInfo.Length)}");
        Console.WriteLine($"📅 Uploaded at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

        return UploadResult.Succeeded(filePath, publishId ?? string.Empty);
    }

    /// <summary>
    /// Batch upload multiple videos
    /// </summary>
    public async Task<BatchUploadResult> UploadBatchAsync(
        string directoryPath,
        string searchPattern = "*.mp4",
        bool autoRefreshTokenIfNeeded = true)
    {
        Console.WriteLine($"\n{'='*60}");
        Console.WriteLine($"📦 Batch Upload: {directoryPath}");
        Console.WriteLine($"{'='*60}");

        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"❌ Directory not found: {directoryPath}");
            return new BatchUploadResult
            {
                DirectoryPath = directoryPath,
                TotalFiles = 0,
                SuccessCount = 0,
                FailureCount = 0,
                Results = new List<UploadResult>(),
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                Success = false,
                ErrorMessage = "Directory not found"
            };
        }

        var videoFiles = Directory.GetFiles(directoryPath, searchPattern)
            .OrderBy(f => f)
            .ToList();

        if (!videoFiles.Any())
        {
            Console.WriteLine($"❌ No {searchPattern} files found in {directoryPath}");
            return new BatchUploadResult
            {
                DirectoryPath = directoryPath,
                TotalFiles = 0,
                SuccessCount = 0,
                FailureCount = 0,
                Results = new List<UploadResult>(),
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                Success = false,
                ErrorMessage = $"No {searchPattern} files found"
            };
        }

        Console.WriteLine($"📋 Found {videoFiles.Count} video file(s)");
        Console.WriteLine($"");

        var results = new List<UploadResult>();
        int successCount = 0;
        int failureCount = 0;
        var startTime = DateTime.UtcNow;

        foreach (var videoFile in videoFiles)
        {
            var result = await UploadSingleVideoAsync(videoFile, autoRefreshTokenIfNeeded && !result.Success);
            results.Add(result);

            if (result.Success)
            {
                successCount++;
            }
            else
            {
                failureCount++;
            }

            // Small delay between uploads to avoid rate limiting
            if (videoFiles.IndexOf(videoFile) < videoFiles.Count - 1)
            {
                await Task.Delay(2000);
            }
        }

        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;

        Console.WriteLine("\n" + "=" * 60);
        Console.WriteLine("📊 BATCH UPLOAD SUMMARY");
        Console.WriteLine("=" * 60);
        Console.WriteLine($"📁 Total files: {videoFiles.Count}");
        Console.WriteLine($"✅ Success: {successCount}");
        Console.WriteLine($"❌ Failed: {failureCount}");
        Console.WriteLine($"⏱️  Duration: {duration.TotalMinutes:0.00} minutes");
        Console.WriteLine($"🕐 Start: {startTime:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine($"🕐 End: {endTime:yyyy-MM-dd HH:mm:ss} UTC");

        if (successCount > 0)
        {
            Console.WriteLine("\n✅ Successful uploads:");
            foreach (var result in results.Where(r => r.Success))
            {
                Console.WriteLine($"   📹 {Path.GetFileName(result.VideoPath)}");
                Console.WriteLine($"      Publish ID: {result.PublishId}");
                Console.WriteLine($"      Uploaded at: {result.UploadedAt:yyyy-MM-dd HH:mm:ss} UTC");
            }
        }

        if (failureCount > 0)
        {
            Console.WriteLine("\n❌ Failed uploads:");
            foreach (var result in results.Where(r => !r.Success))
            {
                Console.WriteLine($"   📹 {Path.GetFileName(result.VideoPath)}");
                Console.WriteLine($"      Error: {result.ErrorMessage}");
            }
        }

        Console.WriteLine("=" * 60);

        return new BatchUploadResult
        {
            DirectoryPath = directoryPath,
            TotalFiles = videoFiles.Count,
            SuccessCount = successCount,
            FailureCount = failureCount,
            Results = results,
            StartTime = startTime,
            EndTime = endTime,
            Success = successCount > 0,
            ErrorMessage = failureCount > 0 ? $"{failureCount} upload(s) failed" : null
        };
    }

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
}

/// <summary>
/// Batch upload result
/// </summary>
public class BatchUploadResult
{
    public required string DirectoryPath { get; set; }
    public int TotalFiles { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public required List<UploadResult> Results { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public TimeSpan Duration => EndTime - StartTime;
}
