using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lunet.Sass.DartSass;

/// <summary>
/// Main wrapper for the dart-sass compiler.
/// Handles downloading, caching, and executing sass compilation.
/// </summary>
public class DartSassCompiler : IDisposable
{
    private readonly string _version;
    private readonly string _cacheDirectory;
    private readonly DartSassDownloader _downloader;
    private string? _executablePath;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance of the DartSassCompiler.
    /// </summary>
    /// <param name="version">Version of dart-sass to use (default: 1.94.0).</param>
    /// <param name="cacheDirectory">Directory to cache the executable (default: app directory).</param>
    public DartSassCompiler(string version = "1.94.0", string? cacheDirectory = null)
    {
        _version = version;
        _cacheDirectory = cacheDirectory ?? Path.Combine(AppContext.BaseDirectory, ".dart-sass", version);
        _downloader = new DartSassDownloader(_version, _cacheDirectory);
    }

    /// <summary>
    /// Ensures the dart-sass executable is downloaded and ready.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_executablePath != null)
            return;

        _executablePath = await _downloader.GetExecutablePathAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Compiles SCSS/Sass to CSS using the provided options.
    /// </summary>
    public async Task<DartSassResult> CompileAsync(DartSassOptions options, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        var args = options.BuildArguments();
        
        var startInfo = new ProcessStartInfo
        {
            FileName = _executablePath,
            RedirectStandardInput = !string.IsNullOrWhiteSpace(options.Stdin),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        startInfo.ArgumentList.Add(Path.Combine(Path.GetDirectoryName(_executablePath)!, "sass.snapshot"));

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };
        
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                errorBuilder.AppendLine(e.Data);
        };

        try
        {
            process.Start();
            
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Write stdin if provided
            if (!string.IsNullOrWhiteSpace(options.Stdin))
            {
                await process.StandardInput.WriteAsync(options.Stdin).ConfigureAwait(false);
                process.StandardInput.Close();
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();

            if (process.ExitCode != 0)
            {
                throw new DartSassCompilationException(
                    $"Sass compilation failed with exit code {process.ExitCode}. {error}{output}",
                    process.ExitCode,
                    error,
                    output);
            }

            return new DartSassResult
            {
                Css = output,
                Stderr = error,
                ExitCode = process.ExitCode,
                Success = true
            };
        }
        catch (Exception ex) when (ex is not DartSassException)
        {
            throw new DartSassException($"Failed to execute sass compiler: {ex.Message}", ex);
        }
    }

    public DartSassResult CompileFile(string inputFile,
        string? outputFile = null,
        Action<DartSassOptions>? configureOptions = null,
        CancellationToken cancellationToken = default)
    {
        var result = Task.Run(async () =>
        {
            var result = await CompileFileAsync(inputFile, outputFile, configureOptions, cancellationToken).ConfigureAwait(false);
            return result;
        }, cancellationToken).GetAwaiter().GetResult();

        return result;
    }
    
    /// <summary>
    /// Compiles SCSS/Sass from a file to CSS.
    /// </summary>
    public Task<DartSassResult> CompileFileAsync(
        string inputFile, 
        string? outputFile = null, 
        Action<DartSassOptions>? configureOptions = null,
        CancellationToken cancellationToken = default)
    {
        var options = new DartSassOptions
        {
            Input = inputFile,
            Output = outputFile
        };

        configureOptions?.Invoke(options);

        return CompileAsync(options, cancellationToken);
    }

    /// <summary>
    /// Compiles SCSS/Sass from a string to CSS.
    /// </summary>
    public DartSassResult CompileString(
        string scss,
        Action<DartSassOptions>? configureOptions = null,
        CancellationToken cancellationToken = default)
    {
        var options = new DartSassOptions
        {
            Stdin = scss
        };

        configureOptions?.Invoke(options);

        var result = Task.Run(async () => await CompileAsync(options, cancellationToken).ConfigureAwait(false), cancellationToken).GetAwaiter().GetResult();
        return result;
    }
    
    /// <summary>
    /// Compiles SCSS/Sass from a string to CSS.
    /// </summary>
    public Task<DartSassResult> CompileStringAsync(
        string scss,
        Action<DartSassOptions>? configureOptions = null,
        CancellationToken cancellationToken = default)
    {
        var options = new DartSassOptions
        {
            Stdin = scss
        };

        configureOptions?.Invoke(options);

        return CompileAsync(options, cancellationToken);
    }

    /// <summary>
    /// Compiles SCSS/Sass from stdin and returns the result to stdout.
    /// </summary>
    public async Task<string> CompileToStringAsync(
        string scss,
        Action<DartSassOptions>? configureOptions = null,
        CancellationToken cancellationToken = default)
    {
        var result = await CompileStringAsync(scss, configureOptions, cancellationToken).ConfigureAwait(false);
        return result.Css;
    }

    /// <summary>
    /// Gets the version of dart-sass being used.
    /// </summary>
    public string Version => _version;

    /// <summary>
    /// Gets the cache directory where the executable is stored.
    /// </summary>
    public string CacheDirectory => _cacheDirectory;

    /// <summary>
    /// Gets whether the compiler has been initialized.
    /// </summary>
    public bool IsInitialized => _executablePath != null;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Result of a dart-sass compilation.
/// </summary>
public class DartSassResult
{
    /// <summary>
    /// The compiled CSS output.
    /// </summary>
    public string Css { get; init; } = string.Empty;

    /// <summary>
    /// Standard error output.
    /// </summary>
    public string Stderr { get; init; } = string.Empty;

    /// <summary>
    /// Exit code of the sass process.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Whether the compilation was successful.
    /// </summary>
    public bool Success { get; init; }
}
