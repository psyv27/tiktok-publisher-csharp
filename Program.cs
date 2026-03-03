using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TikTokPublisher.Services;

namespace TikTokPublisher;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     TikTok Video Publisher - C#/.NET                     ║");
        Console.WriteLine("║     Content Publishing API v2                             ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Console.WriteLine("");

        // Show help if no arguments
        if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
        {
            ShowHelp();
            return 0;
        }

        var command = args[0].ToLower();

        // Setup dependency injection
        var services = ConfigureServices();
        var serviceProvider = services.BuildServiceProvider();

        int exitCode = 0;

        try
        {
            switch (command)
            {
                case "exchange":
                    await ExecuteExchangeCommand(serviceProvider, args);
                    break;

                case "upload":
                case "upload-single":
                    await ExecuteUploadSingleCommand(serviceProvider, args);
                    break;

                case "upload-all":
                case "batch":
                    await ExecuteBatchUploadCommand(serviceProvider, args);
                    break;

                case "info":
                    await ExecuteInfoCommand(serviceProvider);
                    break;

                case "status":
                    await ExecuteStatusCommand(serviceProvider, args);
                    break;

                default:
                    Console.WriteLine($"❌ Unknown command: {command}");
                    Console.WriteLine("   Run with --help for available commands");
                    exitCode = 1;
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fatal error: {ex.Message}");
            Console.WriteLine($"   {ex.StackTrace}");
            exitCode = 1;
        }
        finally
        {
            serviceProvider.Dispose();
        }

        return exitCode;
    }

    static void ShowHelp()
    {
        Console.WriteLine("Usage: dotnet run <command> [options]");
        Console.WriteLine("");
        Console.WriteLine("Commands:");
        Console.WriteLine("  exchange                    Exchange authorization code for access token");
        Console.WriteLine("  upload <file>               Upload single video as draft");
        Console.WriteLine("  upload-all [directory]      Upload all videos from directory");
        Console.WriteLine("  info                        Query creator information");
        Console.WriteLine("  status <publish-id>         Check publish status");
        Console.WriteLine("");
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run exchange");
        Console.WriteLine("  dotnet run upload video.mp4");
        Console.WriteLine("  dotnet run upload-all ../videos/");
        Console.WriteLine("  dotnet run info");
        Console.WriteLine("  dotnet run status v_inbox_file~v2.xxx");
        Console.WriteLine("");
        Console.WriteLine("Configuration:");
        Console.WriteLine("  Tokens are loaded from: Config/api_credentials.json");
    }

    static void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient();

        services.AddSingleton<Services.TikTokApiClient>(sp =>
        {
            var tokenManager = sp.GetRequiredService<TikTokPublisher.Services.TokenManager>();
            tokenManager.LoadCredentials();

            var credentials = tokenManager.GetCredentials();
            if (credentials == null || !credentials.IsValid())
            {
                throw new InvalidOperationException("Invalid or missing credentials");
            }

            var httpClient = sp.GetRequiredService<HttpClient>();
            return new TikTokPublisher.Services.TikTokApiClient(httpClient, credentials);
        });

        services.AddSingleton<Services.TokenManager>();
        services.AddSingleton<Services.VideoUploader>();
    }

    static async Task ExecuteExchangeCommand(IServiceProvider serviceProvider, string[] args)
    {
        Console.WriteLine("🔐 OAuth - Exchange Authorization Code for Token");
        Console.WriteLine("="*60);

        var tokenManager = serviceProvider.GetRequiredService<Services.TokenManager>();

        if (!tokenManager.LoadCredentials())
        {
            Console.WriteLine("❌ Failed to load credentials");
            Console.WriteLine("   Please configure api_credentials.json first");
            return;
        }

        var authUrl = tokenManager.GenerateAuthorizationUrl();
        Console.WriteLine("1️⃣  Open this URL in your browser:");
        Console.WriteLine("");
        Console.WriteLine(authUrl);
        Console.WriteLine("");
        Console.WriteLine("2️⃣  Login and authorize the app");
        Console.WriteLine("");
        Console.WriteLine("3️⃣  Copy the full callback URL from the browser");
        Console.WriteLine("");

        Console.Write("📋 Paste the callback URL: ");
        var callbackUrl = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(callbackUrl))
        {
            Console.WriteLine("❌ No callback URL provided");
            return;
        }

        var authCode = TikTokPublisher.Services.TokenManager.ExtractAuthorizationCode(callbackUrl);

        if (string.IsNullOrEmpty(authCode))
        {
            Console.WriteLine("❌ Failed to extract authorization code from URL");
            return;
        }

        Console.WriteLine($"✅ Authorization code extracted: {authCode[0..20]}...");

        var apiClient = serviceProvider.GetRequiredService<Services.TikTokApiClient>();
        var tokenResponse = await apiClient.ExchangeCodeForTokenAsync(authCode);

        if (tokenResponse == null || tokenResponse.HasError)
        {
            Console.WriteLine("❌ Token exchange failed");
            return;
        }

        var saved = tokenManager.UpdateTokens(tokenResponse);

        if (saved)
        {
            Console.WriteLine("✅ Tokens saved successfully!");
            Console.WriteLine("");
            Console.WriteLine("You can now use other commands to upload videos");
        }
        else
        {
            Console.WriteLine("❌ Failed to save tokens");
        }
    }

    static async Task ExecuteUploadSingleCommand(IServiceProvider serviceProvider, string[] args)
    {
        Console.WriteLine("📤 Upload Single Video");
        Console.WriteLine("="*60);

        string filePath;

        if (args.Length > 1)
        {
            filePath = args[1];
        }
        else
        {
            Console.Write("📹 Enter video file path: ");
            filePath = Console.ReadLine() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Console.WriteLine("❌ No file path provided");
            return;
        }

        var uploader = serviceProvider.GetRequiredService<Services.VideoUploader>();
        var result = await uploader.UploadSingleVideoAsync(filePath);

        if (result == null || !result.Success)
        {
            Console.WriteLine($"❌ Upload failed for: {Path.GetFileName(filePath)}");
            return;
        }

        Console.WriteLine("");
        Console.WriteLine("📋 Upload Result:");
        Console.WriteLine($"   File: {Path.GetFileName(result.VideoPath)}");
        Console.WriteLine($"   Publish ID: {result.PublishId}");
        Console.WriteLine($"   Uploaded at: {result.UploadedAt:yyyy-MM-dd HH:mm:ss} UTC");
    }

    static async Task ExecuteBatchUploadCommand(IServiceProvider serviceProvider, string[] args)
    {
        Console.WriteLine("📦 Batch Upload");
        Console.WriteLine("="*60);

        string directoryPath;

        if (args.Length > 1)
        {
            directoryPath = args[1];
        }
        else
        {
            Console.Write("📁 Enter directory path: ");
            directoryPath = Console.ReadLine() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            // Use default directory
            directoryPath = "../youtube-shorts/downloads/";
            Console.WriteLine($"Using default directory: {directoryPath}");
        }

        var uploader = serviceProvider.GetRequiredService<Services.VideoUploader>();
        await uploader.UploadBatchAsync(directoryPath);
    }

    static async Task ExecuteInfoCommand(IServiceProvider serviceProvider)
    {
        Console.WriteLine("ℹ️  Creator Info");
        Console.WriteLine("="*60);

        var apiClient = serviceProvider.GetRequiredService<Services.TikTokApiClient>();
        await apiClient.GetCreatorInfoAsync();
    }

    static async Task ExecuteStatusCommand(IServiceProvider serviceProvider, string[] args)
    {
        Console.WriteLine("📊 Publish Status");
        Console.WriteLine("="*60);

        string publishId;

        if (args.Length > 1)
        {
            publishId = args[1];
        }
        else
        {
            Console.Write("📋 Enter Publish ID: ");
            publishId = Console.ReadLine() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(publishId))
        {
            Console.WriteLine("❌ No Publish ID provided");
            return;
        }

        var apiClient = serviceProvider.GetRequiredService<Services.TikTokApiClient>();
        await apiClient.CheckPublishStatusAsync(publishId);
    }
}
