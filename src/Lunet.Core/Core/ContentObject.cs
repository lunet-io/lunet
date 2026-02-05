// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Lunet.Helpers;
using Lunet.Scripts;
using Scriban.Functions;
using Scriban.Helpers;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;
using Zio;

namespace Lunet.Core;

/// <summary>
/// Base class for all content object.
/// </summary>
public abstract class ContentObject : TemplateObject
{
    private ContentType _contentType;
    private Dictionary<string, ScriptExpression> _mapTextToReplaceToDynamicExpression;

    protected ContentObject(SiteObject site, ContentObjectType objectType, in FileSystemItem sourceFileInfo = default, ScriptInstance scriptInstance = null, UPath? path = null) : base(site, objectType, sourceFileInfo, scriptInstance, path)
    {
    }
        
    public string Section { get; protected set; }

    public UPath PathInSection { get; protected set; }


    public bool Discard
    {
        get => GetSafeValue<bool>(FileVariables.Discard);
        set => this[FileVariables.Discard] = value;
    }

    /// <summary>
    /// Gets or sets the output of the script.
    /// </summary>
    public string Content
    {
        get => GetSafeValue<string>(PageVariables.Content);
        set => this[PageVariables.Content] = value;
    }

    public string GetOrLoadContent()
    {
        var content = Content;
        if (content == null)
        {
            content = SourceFile.ReadAllText();
            Content = content;
        }

        return content;
    }

    /// <summary>
    /// Gets or sets the summary of this page.
    /// </summary>
    public string Summary
    {
        get => GetSafeValue<string>(PageVariables.Summary);
        set => this[PageVariables.Summary] = value;
    }

    /// <summary>
    /// Gets or sets the output extension. If null, by default the same as the input <see cref="Extension"/>.
    /// </summary>
    public ContentType ContentType
    {
        get { return _contentType; }

        set
        {
            _contentType = value;
            // Special case, ContentType is seen as readonly in scripts
            SetValue(PageVariables.ContentType, _contentType.Name, true);
        }
    }

    // Url with Site.BasePath
    public string Url
    {
        get => GetSafeValue<string>(PageVariables.Url);
        set => this[PageVariables.Url] = value;
    }

    // Url without BasePath
    public string UrlWithoutBasePath
    {
        get => GetSafeValue<string>(PageVariables.UrlWithoutBasePath);
        set => this[PageVariables.UrlWithoutBasePath] = value;
    }

    public string Uid
    {
        get => GetSafeValue<string>(PageVariables.Uid);
        set => this[PageVariables.Uid] = value;
    }

    public DateTime Date
    {
        get
        {
            // Try to recover the date from the current value
            var value = this[PageVariables.Date];
            if (value is DateTime dt) return dt;
            if (value is string str)
            {
                if (DateTime.TryParse(str, out var result))
                {
                    return result;
                }
            }
            // Date is empty
            return new DateTime();
        }
        set => this[PageVariables.Date] = value;
    }

    public int Weight
    {
        get => GetSafeValue<int>(PageVariables.Weight);
        set => this[PageVariables.Weight] = value;
    }

    public string Title
    {
        get => GetSafeValue<string>(PageVariables.Title);
        set => this[PageVariables.Title] = value;
    }

    public string Slug
    {
        get => GetSafeValue<string>(PageVariables.Slug) ?? StringFunctions.Handleize(Title);
        set => this[PageVariables.Slug] = value;
    }

    public string Layout
    {
        get => GetSafeValue<string>(PageVariables.Layout);
        set => this[PageVariables.Layout] = value;
    }

    public string LayoutType
    {
        get => GetSafeValue<string>(PageVariables.LayoutType);
        set => this[PageVariables.LayoutType] = value;
    }

    /// <summary>
    /// Final fix-up for URL of the page once frontmatter has been loaded
    ///
    /// Transforms an URL to a file to a folder URL:
    /// 
    /// From: /blog/this-is-a-post.md
    /// To: /blog/this-is-a-post/
    ///
    /// Or if the index.html/index.md or readme.html or readme.md is used, it converts to the parent folder:
    ///
    /// From: /section/readme.md
    /// To: /section/
    /// 
    /// </summary>
    public void Initialize()
    {
        if (FrontMatter != null)
        {
            Site.Scripts.TryRunFrontMatter(FrontMatter, this);
        }

        // Extract the default Url of this content
        // By default, for html content, we don't output a file but a directory
        var url = Url ?? (string)Path;

        url = ReplaceUrlPlaceHolders(url);
        // In case place holders are all wrong
        if (url == "/")
        {
            url = (string)Path;
        }
        
        // Don't try to patch an URL that is already specifying a "folder" url
        if (!url.EndsWith("/"))
        {
            // Special case handling, if the content is going to be Html,
            // we process its URL to map to a folder if necessary (if e.g this is index.html or readme.md)
            var isHtml = Site.ContentTypes.IsHtmlContentType(ContentType);
            // If the URL ends with HTML, we should leave it as it is.
            if (isHtml && !url.EndsWith(".html"))
            {
                var urlAsPath = (UPath)url;
                var name = urlAsPath.GetNameWithoutExtension();
                var isIndex = name == "index" || (Site.ReadmeAsIndex && name.ToLowerInvariant() == "readme");
                if (isIndex)
                {
                    url = urlAsPath.GetDirectory().FullName;
                    if (!url.EndsWith("/"))
                    {
                        url += "/";
                    }
                }
                else if (HasFrontMatter && !Site.UrlAsFile)
                {
                    var extension = System.IO.Path.GetExtension(url);
                    if (!string.IsNullOrEmpty(extension))
                    {
                        url = url.Substring(0, url.Length - extension.Length);
                    }

                    url = PathUtil.NormalizeUrl(url, true);
                }
            }
        }
            
        var basePath = Site.BasePath;
        string urlWithoutBasePath = url;
        if (!string.IsNullOrEmpty(basePath))
        {
            // Normalize base path
            if (!basePath.StartsWith('/'))
            {
                basePath = "/" + basePath;
            }

            if (basePath.EndsWith('/'))
            {
                basePath = basePath.TrimEnd('/');
            }

            var urlWithBasePath = UPath.Combine(basePath, "." + url);
            url = url.EndsWith('/') ? $"{urlWithBasePath.FullName}/" : url;
        }

        Url = url;
        UrlWithoutBasePath = urlWithoutBasePath;
    }

    public void ChangeContentType(ContentType newContentType)
    {
        ContentType = newContentType;
        var newExtension = ContentType.Name;
        if (ContentType == ContentType.Html)
        {
            newExtension = Site.GetSafeDefaultPageExtension();
        }

        if (!Url.EndsWith("/"))
        {
            Url = System.IO.Path.ChangeExtension(Url, newExtension);
            UrlWithoutBasePath = System.IO.Path.ChangeExtension(UrlWithoutBasePath, newExtension);
        }
    }

    public UPath GetDestinationDirectory()
    {
        var dir = GetDestinationPath().GetDirectory();
        return dir.IsNull ? UPath.Root : dir;
    }

    public UPath GetDestinationPath()
    {
        var urlAsPath = UrlWithoutBasePath;

        Uri uri;
        if (!Uri.TryCreate(urlAsPath, UriKind.Relative, out uri))
        {
            Site.Warning($"Invalid Url [{urlAsPath}] for page [{Path}]. Reverting to page default.");
            urlAsPath = Url = Path.FullName;
        }

        if (ContentType.IsHtmlLike() && !Site.UrlAsFile && urlAsPath.EndsWith("/"))
        {
            urlAsPath += "index" + Site.GetSafeDefaultPageExtension();
        }

        return urlAsPath;
    }

    /// <summary>
    /// Add a deferred expression to be evaluated later and replace the specified text in the content.
    /// </summary>
    /// <param name="textToReplace">The text to replace in the content.</param>
    /// <param name="expression">The expression to evaluate.</param>
    public void AddDefer(string textToReplace, ScriptExpression expression)
    {
        if (_mapTextToReplaceToDynamicExpression is null)
        {
            _mapTextToReplaceToDynamicExpression = new Dictionary<string, ScriptExpression>();
        }
        _mapTextToReplaceToDynamicExpression[textToReplace] = expression;
    }

    /// <summary>
    /// Applies deferred replacements to the content by evaluating dynamic expressions and updating the content
    /// accordingly.
    /// </summary>
    /// <remarks>This method evaluates expressions mapped to specific text placeholders and replaces the
    /// placeholders in the content  with the evaluated results. If the content or the mapping dictionary is null, or if
    /// the mapping dictionary is empty,  the method performs no operation. After processing, the mapping dictionary is
    /// cleared.</remarks>
    public void ApplyDefer()
    {
        if (Content is null || _mapTextToReplaceToDynamicExpression is null || _mapTextToReplaceToDynamicExpression.Count == 0) return;

        foreach (var pair in _mapTextToReplaceToDynamicExpression)
        {
            if (Site.Scripts.TryEvaluateExpression(this, pair.Value, out var result))
            {
                Content = Content.Replace(pair.Key, result, StringComparison.Ordinal);
            }
        }

        _mapTextToReplaceToDynamicExpression.Clear();
    }

    internal void InitializeAfterRun()
    {
        // Normalize the date (make sure it's a datetime object)
        this.Date = Date;
    }

    static readonly Regex PlaceHolderRegex = new Regex(@"(/?):(\w+)");

    private string ReplaceUrlPlaceHolders(string url)
    {
        if (!url.Contains(":")) return url;

        var urlClean = PlaceHolderRegex.Replace(url, evaluator: PlaceHolderRegexEvaluatorWrap);

        if (!urlClean.StartsWith("/"))
        {
            urlClean = $"/{urlClean}";
        }

        return urlClean;
    }


    private string PlaceHolderRegexEvaluatorWrap(Match match)
    {
        var result = PlaceHolderRegexEvaluator(match);
        return string.IsNullOrEmpty(result) ? string.Empty : $"{match.Groups[1].Value}{result}";
    }

    private string PlaceHolderRegexEvaluator(Match match)
    {
        var placeHolder = match.Groups[2].Value;
        switch (placeHolder)
        {
            case "year":
                return Date.ToString("yyyy", CultureInfo.InvariantCulture);
            case "short_year":
                return Date.ToString("yy", CultureInfo.InvariantCulture);
            case "month":
                return Date.ToString("MM", CultureInfo.InvariantCulture);
            case "i_month":
                return Date.ToString("M", CultureInfo.InvariantCulture);
            case "short_month":
                return Date.ToString("MMM", CultureInfo.InvariantCulture);
            case "long_month":
                return Date.ToString("MMMM", CultureInfo.InvariantCulture);
            case "day":
                return Date.ToString("dd", CultureInfo.InvariantCulture);
            case "i_day":
                return Date.ToString("d", CultureInfo.InvariantCulture);
            case "y_day":
                return Date.DayOfYear.ToString("000", CultureInfo.InvariantCulture);
            case "w_year":
                return ISOWeek.GetWeekOfYear(Date).ToString("00", CultureInfo.InvariantCulture);
            case "week":
                return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(Date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday).ToString("00", CultureInfo.InvariantCulture);
            case "w_day":
                var day = (int)Date.DayOfWeek;
                // monday is 1
                // sunday is 7
                day = day == 0 ? 7 : day;
                return day.ToString(CultureInfo.InvariantCulture);
            case "short_day":
                return Date.ToString("ddd", CultureInfo.InvariantCulture);
            case "long_day":
                return Date.ToString("dddd", CultureInfo.InvariantCulture);
            case "hour":
                return Date.ToString("HH", CultureInfo.InvariantCulture);
            case "minute":
                return Date.ToString("mm", CultureInfo.InvariantCulture);
            case "second":
                return Date.ToString("ss", CultureInfo.InvariantCulture);
            case "title":
                return StringFunctions.Handleize(Title);
            case "slug":
                return Slug;
            case "section":
                return Section;
            case "slugified_section":
                return StringFunctions.Handleize(Section);
            case "output_ext":
                return Extension;
            case "path":
                return Path.FullName;
        }

        var pos = new TextPosition(0, 1, 1);
        Site.Warning(new SourceSpan(Path.FullName, pos, pos), $"The URL placeholder `{placeHolder}` is not supported.");

        return null;
    }

}


[DebuggerDisplay("File: {" + nameof(Path) + "}")]
public class FileContentObject : ContentObject
{
    private static readonly Regex ParsePostName = new Regex(@"^(\d{4})-(\d{2})-(\d{2})-(.+)\..+$");

    public FileContentObject(SiteObject site, in FileSystemItem sourceFileInfo, ScriptInstance scriptInstance = null, UPath? path = null, ScriptObject preContent = null) : base(site, ContentObjectType.File, sourceFileInfo, scriptInstance, path)
    {
        preContent?.CopyTo(this);

        // TODO: Make this part pluggable
        // Parse a standard blog text
        var match = ParsePostName.Match(sourceFileInfo.GetName());
        if (match.Success)
        {
            var year = int.Parse(match.Groups[1].Value);
            var month = int.Parse(match.Groups[2].Value);
            var day = int.Parse(match.Groups[3].Value);
            var title = match.Groups[4].Value;
            Date = new DateTime(year, month, day);
            Slug = title;
            Title = StringFunctions.Capitalize(title.Replace('-', ' '));
        }
        else
        {
            Date = DateTime.Now;
        }

        ContentType = Site.ContentTypes.GetContentType(Extension);

        // Extract the section of this content
        // section cannot be setup by the pre-content
        Section = Path.GetFirstDirectory(out var pathInSection);
        if (pathInSection.IsEmpty)
        {
            Section = string.Empty;
            PathInSection = Path;
        }
        else
        {
            PathInSection = pathInSection;
        }

        // Layout could have been already setup by pre-content, so we keep it in that case
        Layout ??= Section;

        // Replicate readonly values to the Scripting object
        InitializeReadOnlyVariables();
    }

    private void InitializeReadOnlyVariables()
    {
        // Replicate readonly values to the Scripting object
        SetValue(FileVariables.Length, Length, true);
        SetValue(FileVariables.ModifiedTime, ModifiedTime, true);
        SetValue(FileVariables.Path, Path, true);
        SetValue(FileVariables.Extension, Extension, true);

        SetValue(PageVariables.Section, Section, true);
        SetValue(PageVariables.PathInSection, PathInSection, true);
    }
}

[DebuggerDisplay("Dynamic: {" + nameof(Url) + "}")]
public class DynamicContentObject : ContentObject
{
    public DynamicContentObject(SiteObject site, string url, string section = null, UPath? path = null) : base(site, ContentObjectType.Dynamic, path: path)
    {
        Url = new UPath($"{site.BasePath}/{url}").FullName;
        UrlWithoutBasePath = url;
        Section = section;

        // Replicate readonly values to the Scripting object
        InitializeReadOnlyVariables();
    }

    private void InitializeReadOnlyVariables()
    {
        SetValue(PageVariables.Section, Section, true);
    }
}