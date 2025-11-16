using System.Collections.Generic;

namespace Lunet.Sass.DartSass;

/// <summary>
/// Output style for the compiled CSS.
/// </summary>
public enum OutputStyle
{
    Expanded,
    Compressed
}

/// <summary>
/// Source map URL style.
/// </summary>
public enum SourceMapUrls
{
    Relative,
    Absolute
}

/// <summary>
/// Package importer type.
/// </summary>
public enum PkgImporterType
{
    Node
}

/// <summary>
/// Options for dart-sass compilation.
/// </summary>
public class DartSassOptions
{
    /// <summary>
    /// Input file path or stdin indicator ('-').
    /// </summary>
    public string? Input { get; set; }

    /// <summary>
    /// Output file path or stdout indicator ('-').
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// Input content for stdin mode.
    /// </summary>
    public string? Stdin { get; set; }

    // === Input and Output ===

    /// <summary>
    /// Use the indented syntax for input from stdin.
    /// </summary>
    public bool Indented { get; set; }

    /// <summary>
    /// Load paths for Sass imports.
    /// </summary>
    public List<string> LoadPaths { get; set; } = new();

    /// <summary>
    /// Built-in importer(s) to use for pkg: URLs.
    /// </summary>
    public PkgImporterType? PkgImporter { get; set; }

    /// <summary>
    /// Output style (expanded or compressed).
    /// </summary>
    public OutputStyle Style { get; set; } = OutputStyle.Expanded;

    /// <summary>
    /// Emit a @charset or BOM for CSS with non-ASCII characters (defaults to on).
    /// </summary>
    public bool? Charset { get; set; }

    /// <summary>
    /// When an error occurs, emit a stylesheet describing it.
    /// </summary>
    public bool? ErrorCss { get; set; }

    /// <summary>
    /// Only compile out-of-date stylesheets.
    /// </summary>
    public bool Update { get; set; }

    // === Source Maps ===

    /// <summary>
    /// Whether to generate source maps (defaults to on).
    /// </summary>
    public bool? SourceMap { get; set; }

    /// <summary>
    /// How to link from source maps to source files.
    /// </summary>
    public SourceMapUrls? SourceMapUrls { get; set; }

    /// <summary>
    /// Embed source file contents in source maps.
    /// </summary>
    public bool? EmbedSources { get; set; }

    /// <summary>
    /// Embed source map contents in CSS.
    /// </summary>
    public bool? EmbedSourceMap { get; set; }

    // === Warnings ===

    /// <summary>
    /// Don't print warnings.
    /// </summary>
    public bool? Quiet { get; set; }

    /// <summary>
    /// Don't print compiler warnings from dependencies.
    /// </summary>
    public bool? QuietDeps { get; set; }

    /// <summary>
    /// Print all deprecation warnings even when they're repetitive.
    /// </summary>
    public bool? Verbose { get; set; }

    /// <summary>
    /// Deprecations to treat as errors. You may also pass a Sass version.
    /// </summary>
    public List<string> FatalDeprecation { get; set; } = new();

    /// <summary>
    /// Deprecations to ignore.
    /// </summary>
    public List<string> SilenceDeprecation { get; set; } = new();

    /// <summary>
    /// Opt in to a deprecation early.
    /// </summary>
    public List<string> FutureDeprecation { get; set; } = new();

    // === Other ===

    /// <summary>
    /// Watch stylesheets and recompile when they change.
    /// </summary>
    public bool Watch { get; set; }

    /// <summary>
    /// Manually check for changes rather than using a native watcher (only valid with --watch).
    /// </summary>
    public bool? Poll { get; set; }

    /// <summary>
    /// Don't compile more files once an error is encountered.
    /// </summary>
    public bool? StopOnError { get; set; }

    /// <summary>
    /// Run an interactive SassScript shell.
    /// </summary>
    public bool Interactive { get; set; }

    /// <summary>
    /// Whether to use terminal colors for messages.
    /// </summary>
    public bool? Color { get; set; }

    /// <summary>
    /// Whether to use Unicode characters for messages.
    /// </summary>
    public bool? Unicode { get; set; }

    /// <summary>
    /// Print full Dart stack traces for exceptions.
    /// </summary>
    public bool? Trace { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to get the version of dart.
    /// </summary>
    public bool Version { get; set; }

    /// <summary>
    /// Additional custom arguments to pass to the sass executable.
    /// </summary>
    public List<string> CustomArguments { get; set; } = new();

    /// <summary>
    /// Builds the command-line arguments from the options.
    /// </summary>
    internal List<string> BuildArguments()
    {
        var args = new List<string>();

        if (Version)
        {
            args.Add("--version");
            return args;
        }

        // === Input and Output ===

        if (!string.IsNullOrEmpty(Stdin))
            args.Add("--stdin");

        if (Indented)
            args.Add("--indented");

        foreach (var loadPath in LoadPaths)
        {
            args.Add("--load-path");
            args.Add(loadPath);
        }

        if (PkgImporter.HasValue)
        {
            args.Add($"--pkg-importer={PkgImporter.Value.ToString().ToLowerInvariant()}");
        }

        // Style
        args.Add($"--style={Style.ToString().ToLowerInvariant()}");

        // Charset
        if (Charset.HasValue)
        {
            args.Add(Charset.Value ? "--charset" : "--no-charset");
        }

        // Error CSS
        if (ErrorCss.HasValue)
        {
            args.Add(ErrorCss.Value ? "--error-css" : "--no-error-css");
        }

        if (Update)
            args.Add("--update");

        // === Source Maps ===

        if (SourceMap.HasValue)
        {
            args.Add(SourceMap.Value ? "--source-map" : "--no-source-map");
        }

        if (SourceMapUrls.HasValue)
        {
            args.Add($"--source-map-urls={SourceMapUrls.Value.ToString().ToLowerInvariant()}");
        }

        if (EmbedSources.HasValue)
        {
            args.Add(EmbedSources.Value ? "--embed-sources" : "--no-embed-sources");
        }

        if (EmbedSourceMap.HasValue)
        {
            args.Add(EmbedSourceMap.Value ? "--embed-source-map" : "--no-embed-source-map");
        }

        // === Warnings ===

        if (Quiet.HasValue)
        {
            args.Add(Quiet.Value ? "--quiet" : "--no-quiet");
        }

        if (QuietDeps.HasValue)
        {
            args.Add(QuietDeps.Value ? "--quiet-deps" : "--no-quiet-deps");
        }

        if (Verbose.HasValue)
        {
            args.Add(Verbose.Value ? "--verbose" : "--no-verbose");
        }

        foreach (var deprecation in FatalDeprecation)
        {
            args.Add("--fatal-deprecation");
            args.Add(deprecation);
        }

        foreach (var deprecation in SilenceDeprecation)
        {
            args.Add("--silence-deprecation");
            args.Add(deprecation);
        }

        foreach (var deprecation in FutureDeprecation)
        {
            args.Add("--future-deprecation");
            args.Add(deprecation);
        }

        // === Other ===

        if (Watch)
            args.Add("--watch");

        if (Poll.HasValue)
        {
            args.Add(Poll.Value ? "--poll" : "--no-poll");
        }

        if (StopOnError.HasValue)
        {
            args.Add(StopOnError.Value ? "--stop-on-error" : "--no-stop-on-error");
        }

        if (Interactive)
            args.Add("--interactive");

        if (Color.HasValue)
        {
            args.Add(Color.Value ? "--color" : "--no-color");
        }

        if (Unicode.HasValue)
        {
            args.Add(Unicode.Value ? "--unicode" : "--no-unicode");
        }

        if (Trace.HasValue)
        {
            args.Add(Trace.Value ? "--trace" : "--no-trace");
        }

        // Custom arguments
        args.AddRange(CustomArguments);

        // Input and output (must be last)
        if (!string.IsNullOrWhiteSpace(Input))
            args.Add(Input);
        if (!string.IsNullOrWhiteSpace(Output))
            args.Add(Output);

        return args;
    }
}
