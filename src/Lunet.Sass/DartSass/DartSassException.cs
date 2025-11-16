using System;

namespace Lunet.Sass.DartSass;

/// <summary>
/// Base exception for all dart-sass related errors.
/// </summary>
public class DartSassException : Exception
{
    public DartSassException(string message) : base(message)
    {
    }

    public DartSassException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when downloading the dart-sass executable fails.
/// </summary>
public class DartSassDownloadException : DartSassException
{
    public string? Url { get; }

    public DartSassDownloadException(string message, string? url = null) : base(message)
    {
        Url = url;
    }

    public DartSassDownloadException(string message, Exception innerException, string? url = null) 
        : base(message, innerException)
    {
        Url = url;
    }
}

/// <summary>
/// Exception thrown when extracting the dart-sass archive fails.
/// </summary>
public class DartSassExtractionException : DartSassException
{
    public string? ArchivePath { get; }

    public DartSassExtractionException(string message, string? archivePath = null) : base(message)
    {
        ArchivePath = archivePath;
    }

    public DartSassExtractionException(string message, Exception innerException, string? archivePath = null) 
        : base(message, innerException)
    {
        ArchivePath = archivePath;
    }
}

/// <summary>
/// Exception thrown when sass compilation fails.
/// </summary>
public class DartSassCompilationException : DartSassException
{
    public int ExitCode { get; }
    public string? StandardError { get; }
    public string? StandardOutput { get; }

    public DartSassCompilationException(string message, int exitCode, string? standardError = null, string? standardOutput = null) 
        : base(message)
    {
        ExitCode = exitCode;
        StandardError = standardError;
        StandardOutput = standardOutput;
    }

}
