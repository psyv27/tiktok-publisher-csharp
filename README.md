# TikTok Video Publisher - C#/.NET

🤖 Automated TikTok video uploader using official Content Publishing API v2 built with C#/.NET

## Features

- ✅ **Official TikTok API** - Content Publishing API v2 integration
- ✅ **OAuth 2.0** - Secure authentication with token refresh
- ✅ **Draft Upload** - Upload videos as drafts (manual publishing)
- ✅ **Batch Upload** - Upload all videos from directory
- ✅ **Token Management** - Automated access token refresh
- ✅ **CLI Interface** - Command-line interface for all operations
- ✅ **.NET 8.0** - Built with modern C# features

## Requirements

- .NET 8.0 SDK
- [TikTok Developer Account](https://developers.tiktok.com)
- App with Content Posting API enabled
- Approved scopes: `video.upload` (for drafts) or `video.publish` (for direct publishing)

## Installation

1. **Clone repository**
```bash
git clone https://github.com/YOUR_USERNAME/tiktok-publisher-csharp.git
cd tiktok-publisher-csharp
```

2. **Install .NET SDK** (if not installed)
```bash
# Linux/macOS
curl -sSL https://dot.net/v1/dotnet-install.sh | bash

# Windows
# Download from: https://dotnet.microsoft.com/download
```

3. **Restore NuGet packages**
```bash
dotnet restore
```

4. **Copy config template**
```bash
cp Config/api_credentials.json.example Config/api_credentials.json
```

## Setup

### 1. Create TikTok App

1. Go to [TikTok Developer Portal](https://developers.tiktok.com)
2. Create a new app
3. Enable **Content Posting API** product
4. Save your `CLIENT_ID` and `CLIENT_SECRET`

### 2. Get OAuth Authorization Code

Open this URL in your browser:

```
https://www.tiktok.com/v2/auth/authorize/?client_key=YOUR_CLIENT_ID&redirect_uri=https://oauth.pstmn.io/v1/callback&response_type=code&scope=video.upload&state=123
```

### 3. Exchange Code for Token

Using the CLI:
```bash
dotnet run exchange
```

The script will:
1. Show the OAuth authorization URL
2. Prompt you to paste the callback URL from the browser
3. Exchange the code for access and refresh tokens
4. Save tokens to `Config/api_credentials.json`

### 4. Configure Credentials

Edit `Config/api_credentials.json`:

```json
{
  "ClientId": "your_client_id",
  "ClientSecret": "your_client_secret",
  "RedirectUri": "https://oauth.pstmn.io/v1/callback",
  "AccessToken": "act...",
  "RefreshToken": "rft...",
  "OpenId": "-000A...",
  "ExpiresIn": 86400,
  "RefreshExpiresIn": 31536000,
  "Scopes": "video.upload,user.info.basic",
  "Status": "configured"
}
```

## Usage

### CLI Commands

**Show help:**
```bash
dotnet run -- --help
```

**Exchange authorization code:**
```bash
dotnet run exchange
```

**Upload single video:**
```bash
dotnet run upload video.mp4
# Or prompt for file:
dotnet run upload
```

**Batch upload all videos:**
```bash
dotnet run upload-all ../videos/
# Or use default directory:
dotnet run upload-all
```

**Query creator info:**
```bash
dotnet run info
```

**Check publish status:**
```bash
dotnet run status v_inbox_file~v2.7612993423670380561
```

### Results

After upload:
1. Open TikTok app on your phone
2. Navigate to **Inbox / Drafts**
3. Your uploaded videos will be ready to publish

### Publish ID

Each uploaded video gets a `publish_id`, for example:
`v_inbox_file~v2.7612993423670380561`

Console output example:
```
📦 Publish ID: v_inbox_file~v2.7612993423670380561
📹 Video: TikTok video #7588920065173556502.mp4
📁 Size: 354 KB
📅 Uploaded at: 2026-03-03 13:15:30 UTC
```

## File Structure

```
tiktok-publisher-csharp/
├── tiktok-publisher-csharp.csproj    # .NET project file
├── Program.cs                        # Main entry point & CLI
├── Models/                           # Data models
│   ├── Credentials.cs               # Credentials model
│   ├── TokenResponse.cs             # OAuth response
│   ├── PublishResponse.cs           # Publish API response
│   └── VideoUploadRequest.cs        # Upload request
├── Services/                         # Business logic
│   ├── TikTokApiClient.cs           # TikTok API client
│   ├── TokenManager.cs              # OAuth token management
│   └── VideoUploader.cs             # Upload logic
├── Config/                           # Configuration
│   ├── api_credentials.json        # Credentials (NOT in git!)
│   └── api_credentials.json.example # Template
├── README.md                        # This file
└── .gitignore                       # Excludes videos, secrets
```

## API Endpoints

### TikTok Content Publishing API v2

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/v2/post/publish/inbox/video/init/` | POST | Initialize draft upload |
| `/v2/post/publish/video/init/` | POST | Initialize direct publish |
| `/v2/post/publish/status/fetch/` | POST | Check publish status |
| `/v2/post/publish/creator_info/query/` | POST | Query creator info |

### CDN Upload

Method: `PUT {upload_url}`

Required Headers:
```
Content-Type: video/mp4
Content-Range: bytes 0-{size-1}/{total_size}
```

Example:
```
Content-Range: bytes 0-363244/363245
```

## Limitations

### Unaudited Applications

If your app hasn't passed audit yet:
- ✅ Can publish to **PRIVATE** accounts (direct publish)
- ✅ Can upload drafts to **PUBLIC** accounts
- ❌ Direct publishing to PUBLIC accounts is not allowed

### Video Requirements

- **Format:** `.mp4`, `.mov`
- **Max size:** 500 MB
- **Max duration:** 60 minutes
- **Recommended aspect ratio:** 9:16 (vertical video)

## Troubleshooting

### Missing .NET SDK

```bash
# Install .NET SDK 8.0
curl -sSL https://dot.net/v1/dotnet-install.sh | bash
export PATH=$PATH:~/.dotnet
```

### 416 Range Not Satisfiable

**Problem:** Incorrect Content-Range header format

**Solution:** The library uses exact format:
```
Content-Range: bytes 0-{size-1}/{total_size}
```

### unaudited_client_can_only_post_to_private_accounts

**Problem:** Trying to publish directly to public account

**Solution:**
- Use `video.upload` scope for draft uploads
- OR make your TikTok account private

### Access Token Expired

**Problem:** Access token only valid for 24 hours

**Solution:** The library automatically refreshes tokens using refresh_token

## Security

- ❌ **NOT** commit `Config/api_credentials.json` to git repository
- ❌ **NOT** share access_token or refresh_token
- ✅ Use `api_credentials.json.example` as template
- ✅ Sensitive files are excluded in .gitignore

## Deployment

### Build for Production

```bash
# Build Release
dotnet build --configuration Release

# Publish as self-contained app
dotnet publish -c Release -r linux-x64 --self-contained

# Published output: bin/Release/net8.0/linux-x64/publish/
```

### Run on Server

```bash
# Clone repo
git clone <repository-url>
cd tiktok-publisher-csharp

# Configure credentials
cp Config/api_credentials.json.example Config/api_credentials.json
nano Config/api_credentials.json

# Build
dotnet build -c Release

# Run
dotnet run --project tiktok-publisher-csharp.csproj -- upload-all
```

### Cron Job (Automatic Upload)

```bash
# Edit crontab
crontab -e

# Run every 6 hours
0 */6 * * * cd /path/to/tiktok-publisher-csharp && ~/.dotnet/dotnet run --project tiktok-publisher-csharp.csproj -- upload-all /path/to/videos/ >> logs/bot.log 2>&1
```

### Docker Deployment

Create `Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "tiktok-publisher-csharp.dll"]
```

Build and run:
```bash
docker build -t tiktok-publisher-csharp .
docker run -v $(pwd)/Config:/app/Config -v $(pwd)/videos:/app/videos tiktok-publisher-csharp upload-all /app/videos
```

## NuGet Dependencies

- `Newtonsoft.Json` - JSON serialization
- `Microsoft.Extensions.Configuration` - Configuration management
- `Microsoft.Extensions.DependencyInjection` - DI container
- `Microsoft.Extensions.Logging` - Logging
- `CliFx` - Command-line interface

## Code Features

- ✅ Dependency Injection with Microsoft.Extensions
- ✅ Async/await throughout
- ✅ Cancellation token support (not fully implemented but ready)
- ✅ Strong typing with C# records
- ✅ Clean error handling
- ✅ Progress logging
- ✅ Token auto-refresh
- ✅ Batch uploading with summary
- ✅ File size formatting
- ✅ Human-friendly CLI output

## API References

- [TikTok Content Posting API](https://developers.tiktok.com/doc/content-posting-api-get-started)
- [TikTok Scopes Overview](https://developers.tiktok.com/doc/scopes-overview)
- [TikTok App Audit](https://developers.tiktok.com/application/content-posting-api)
- [.NET Documentation](https://docs.microsoft.com/dotnet/)

## Example Output

**Single Upload:**
```
╔═══════════════════════════════════════════════════════════╗
║     TikTok Video Publisher - C#/.NET                     ║
║     Content Publishing API v2                             ║
╚═══════════════════════════════════════════════════════════╝

============================================================
📤 Upload Single Video
============================================================

📹 Uploading: TikTok video #7588920065173556502.mp4
============================================================

1️⃣ Initializing upload...
Initialize upload response (Status: OK):
{"data":{"publish_id":"v_inbox_file~v2.7612993423670380561",...}}

2️⃣ Uploading to CDN...
📤 Uploading video file...
   File: TikTok video #7588920065173556502.mp4
   Size: 354.54 KB
CDN Upload Status: Created

✅ Video file uploaded to CDN successfully!

3️⃣ Checking upload status...
Publish status: PUBLISH_IN_PROGRESS (PUBLISH_IN_PROGRESS)

✅ Upload completed successfully!
📦 Publish ID: v_inbox_file~v2.7612993423670380561
📹 Video: TikTok video #7588920065173556502.mp4
📁 Size: 354.54 KB
📅 Uploaded at: 2026-03-03 13:15:30 UTC
```

**Batch Upload:**
```
============================================================
📦 Batch Upload: ../videos/
============================================================
📋 Found 5 video file(s)

============================================================
📊 BATCH UPLOAD SUMMARY
============================================================
📁 Total files: 5
✅ Success: 4
❌ Failed: 1
⏱️  Duration: 2.34 minutes
🕐 Start: 2026-03-03 13:15:00 UTC
🕐 End: 2026-03-03 13:17:23 UTC
```

## License

MIT

## Contributing

PRs welcome! Please:
- Follow C# coding conventions
- Run `dotnet build` before committing
- Update documentation as needed

---

**Built with ❤️ using C#/.NET 8.0** 🚀
