using System;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Lunet.Sass.DartSass;

/// <summary>
/// Handles downloading and caching of dart-sass executables.
/// </summary>
internal class DartSassDownloader
{
    private readonly string _version;
    private readonly string _cacheDirectory;
    private static readonly HttpClient _httpClient = new();
    private static readonly SemaphoreSlim _downloadLock = new(1, 1);

    public DartSassDownloader(string version, string cacheDirectory)
    {
        _version = version;
        _cacheDirectory = cacheDirectory;
    }

    /// <summary>
    /// Gets the path to the sass executable, downloading if necessary.
    /// </summary>
    public async Task<string> GetExecutablePathAsync(CancellationToken cancellationToken = default)
    {
        var platform = GetPlatform();
        var executableName = platform.IsWindows ? "dart.exe" : "dart";
        var executablePath = Path.Combine(_cacheDirectory, "dart-sass", "src", executableName);

        // Check if already cached
        if (File.Exists(executablePath))
        {
            return executablePath;
        }

        // Download and extract
        await _downloadLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check after acquiring lock
            if (File.Exists(executablePath))
            {
                return executablePath;
            }

            await DownloadAndExtractAsync(platform, cancellationToken).ConfigureAwait(false);

            if (!File.Exists(executablePath))
            {
                throw new DartSassExtractionException(
                    $"Executable not found after extraction: {executablePath}");
            }

            return executablePath;
        }
        finally
        {
            _downloadLock.Release();
        }
    }

    private async Task DownloadAndExtractAsync(PlatformInfo platform, CancellationToken cancellationToken)
    {
        var url = BuildDownloadUrl(platform);
        var archivePath = Path.Combine(_cacheDirectory, Path.GetFileName(url));

        try
        {
            // Download
            Directory.CreateDirectory(_cacheDirectory);

            using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new DartSassDownloadException(
                        $"Failed to download dart-sass: HTTP {response.StatusCode}", url);
                }

                await using var fileStream = new FileStream(archivePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
            }

            // Extract
            if (platform.IsWindows)
            {
                ExtractZip(archivePath);
            }
            else
            {
                await ExtractTarGzAsync(archivePath, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (HttpRequestException ex)
        {
            throw new DartSassDownloadException(
                $"Failed to download dart-sass from {url}: {ex.Message}", ex, url);
        }
        catch (Exception ex) when (ex is not DartSassException)
        {
            throw new DartSassExtractionException(
                $"Failed to extract dart-sass archive: {ex.Message}", ex, archivePath);
        }
        finally
        {
            // Clean up archive
            if (File.Exists(archivePath))
            {
                try
                {
                    File.Delete(archivePath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    private void ExtractZip(string archivePath)
    {
        try
        {
            ZipFile.ExtractToDirectory(archivePath, _cacheDirectory, overwriteFiles: true);
        }
        catch (Exception ex)
        {
            throw new DartSassExtractionException(
                $"Failed to extract ZIP archive: {ex.Message}", ex, archivePath);
        }
    }

    private async Task ExtractTarGzAsync(string archivePath, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new GZipStream(File.Open(archivePath, FileMode.Open, FileAccess.Read), CompressionMode.Decompress, false);
            await TarFile.ExtractToDirectoryAsync(stream, _cacheDirectory, true, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new DartSassExtractionException($"Failed to extract tar.gz archive: {ex.Message}", ex, archivePath);
        }
    }
    
    private string BuildDownloadUrl(PlatformInfo platform)
    {
        var baseUrl = $"https://github.com/sass/dart-sass/releases/download/{_version}";
        var fileName = $"dart-sass-{_version}-{platform.Identifier}.{platform.Extension}";
        return $"{baseUrl}/{fileName}";
    }

    private static PlatformInfo GetPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            var arch = RuntimeInformation.ProcessArchitecture;
            return arch switch
            {
                Architecture.X64 => new PlatformInfo("windows-x64", "zip", true),
                Architecture.Arm64 => new PlatformInfo("windows-arm64", "zip", true),
                _ => throw new PlatformNotSupportedException($"Unsupported Windows architecture: {arch}")
            };
        }
        
        if (OperatingSystem.IsLinux())
        {
            var arch = RuntimeInformation.ProcessArchitecture;
            var isMusl = RuntimeInformation.RuntimeIdentifier.StartsWith("linux-musl");
            var muslSuffix = isMusl ? "-musl" : "";

            return arch switch
            {
                Architecture.X64 => new PlatformInfo($"linux-x64{muslSuffix}", "tar.gz", false),
                Architecture.Arm64 => new PlatformInfo($"linux-arm64{muslSuffix}", "tar.gz", false),
                Architecture.Arm => new PlatformInfo($"linux-arm{muslSuffix}", "tar.gz", false),
                _ => throw new PlatformNotSupportedException($"Unsupported Linux architecture: {arch}")
            };
        }

        if (OperatingSystem.IsWindows())
        {
            var arch = RuntimeInformation.ProcessArchitecture;
            return arch switch
            {
                Architecture.X64 => new PlatformInfo("macos-x64", "tar.gz", false),
                Architecture.Arm64 => new PlatformInfo("macos-arm64", "tar.gz", false),
                _ => throw new PlatformNotSupportedException($"Unsupported macOS architecture: {arch}")
            };
        }

        throw new PlatformNotSupportedException($"Unsupported operating system {RuntimeInformation.OSDescription}");
    }
    
    private record PlatformInfo(string Identifier, string Extension, bool IsWindows);
}
