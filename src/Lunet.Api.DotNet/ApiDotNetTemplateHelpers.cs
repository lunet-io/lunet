// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Lunet.Core;
using Scriban.Functions;
using Scriban.Runtime;

namespace Lunet.Api.DotNet;

internal sealed class ApiDotNetTemplateHelpers
{
    private const string DefaultMembersClass = "api-dotnet-members list-group list-group-flush";
    private const string XRefOpenTag = "<xref";
    private const string XRefCloseTag = "</xref>";
    private readonly SiteObject _site;
    private readonly Dictionary<string, string> _kindIcons;

    public ApiDotNetTemplateHelpers(SiteObject site, ApiDotNetConfig config)
    {
        _site = site ?? throw new ArgumentNullException(nameof(site));
        ArgumentNullException.ThrowIfNull(config);
        _kindIcons = BuildKindIcons(config.KindIcons);
    }

    public void Register(ScriptObject target)
    {
        ArgumentNullException.ThrowIfNull(target);

        target.Import("xref_to_html_link", (Func<string?, bool, bool, string>)XRefToHtmlLink);
        target.Import("api_dotnet_resolve_xrefs", (Func<string?, bool, string>)ApiDotNetResolveXrefs);
        target.Import("api_dotnet_xref_attr", (Func<string?, string?, string>)ApiDotNetXRefAttr);
        target.Import("api_dotnet_handleize", (Func<string?, string>)ApiDotNetHandleize);
        target.Import("api_dotnet_repo_to_https_url", (Func<string?, string>)ApiDotNetRepoToHttpsUrl);
        target.Import("api_dotnet_source_url", (Func<object?, string>)ApiDotNetSourceUrl);
        target.Import("api_dotnet_kind_icon", (Func<string?, string>)ApiDotNetKindIcon);
        target.Import("api_dotnet_members_render", (Func<ScriptObject?, string, string, string?, string>)ApiDotNetMembersRender);
    }

    public string XRefToHtmlLink(string? uid, bool useFullName = false, bool warnIfNotResolved = true)
    {
        if (uid == null)
        {
            return string.Empty;
        }

        var uidText = uid.Trim();
        if (uidText.Length == 0)
        {
            return string.Empty;
        }

        var isGenericTypeParameter = uidText.Length > 2 && uidText[0] == '{' && uidText[^1] == '}';
        var isNonUidType = uidText.Contains(' ') || uidText.Contains("delegate*", StringComparison.Ordinal);
        var looksLikeUid = uidText.Contains('.') || uidText.Contains(':');

        if (isGenericTypeParameter || isNonUidType || !looksLikeUid)
        {
            return HtmlEscape(uidText);
        }

        if (TryResolveXRef(uidText, useFullName, out var resolvedName, out var resolvedUrl))
        {
            return $"<a href=\"{HtmlEscape(resolvedUrl)}\">{HtmlEscape(resolvedName)}</a>";
        }

        if (_site.Content.Finder.TryGetTitleByUid(uidText, out var knownTitle) && !string.IsNullOrWhiteSpace(knownTitle))
        {
            return HtmlEscape(knownTitle);
        }

        if (warnIfNotResolved)
        {
            _site.Warning($"Unable to find xref for {uidText}");
        }

        return HtmlEscape(uidText);
    }

    public string ApiDotNetResolveXrefs(string? text, bool useFullName = false)
    {
        if (text == null)
        {
            return string.Empty;
        }

        if (text.Length == 0 || !text.Contains(XRefOpenTag, StringComparison.Ordinal))
        {
            return text;
        }

        var result = new StringBuilder(text.Length);
        var position = 0;
        while (position < text.Length)
        {
            var xrefStart = text.IndexOf(XRefOpenTag, position, StringComparison.Ordinal);
            if (xrefStart < 0)
            {
                result.Append(text, position, text.Length - position);
                break;
            }

            if (xrefStart > position)
            {
                result.Append(text, position, xrefStart - position);
            }

            var tagEnd = text.IndexOf('>', xrefStart);
            if (tagEnd < 0)
            {
                result.Append(text, xrefStart, text.Length - xrefStart);
                break;
            }

            var tagLength = tagEnd - xrefStart + 1;
            var tag = text.Substring(xrefStart, tagLength);

            var uid = ApiDotNetXRefAttr(tag, "href");
            if (uid.Length == 0)
            {
                uid = ApiDotNetXRefAttr(tag, "uid");
            }

            var name = ApiDotNetXRefAttr(tag, "name");
            var throwIfNotResolved = ApiDotNetXRefAttr(tag, "data-throw-if-not-resolved");
            var warnIfNotResolved = throwIfNotResolved.Length == 0 || !throwIfNotResolved.Equals("false", StringComparison.OrdinalIgnoreCase);

            if (uid.StartsWith("langword_", StringComparison.Ordinal) && name.Length > 0)
            {
                result.Append("<code>");
                result.Append(HtmlEscape(name));
                result.Append("</code>");
            }
            else if (uid.Length > 0)
            {
                result.Append(XRefToHtmlLink(uid, useFullName, warnIfNotResolved));
            }
            else if (name.Length > 0)
            {
                result.Append(HtmlEscape(name));
            }

            position = tagEnd + 1;
            if (position + XRefCloseTag.Length <= text.Length &&
                string.CompareOrdinal(text, position, XRefCloseTag, 0, XRefCloseTag.Length) == 0)
            {
                position += XRefCloseTag.Length;
            }
        }

        return result.ToString();
    }

    public string ApiDotNetXRefAttr(string? tag, string? attrName)
    {
        if (string.IsNullOrEmpty(tag) || string.IsNullOrEmpty(attrName))
        {
            return string.Empty;
        }

        var needle = $"{attrName}=";
        var position = tag.IndexOf(needle, StringComparison.Ordinal);
        if (position < 0)
        {
            return string.Empty;
        }

        var valueStart = position + needle.Length;
        if (valueStart >= tag.Length)
        {
            return string.Empty;
        }

        var quote = tag[valueStart];
        if (quote != '"' && quote != '\'')
        {
            return string.Empty;
        }

        valueStart++;
        var valueEnd = tag.IndexOf(quote, valueStart);
        if (valueEnd < 0)
        {
            return string.Empty;
        }

        return tag.Substring(valueStart, valueEnd - valueStart);
    }

    public string ApiDotNetHandleize(string? text)
    {
        if (text == null)
        {
            return string.Empty;
        }

        var value = text.Trim().ToLowerInvariant();
        if (value.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var previousIsDash = false;
        foreach (var c in value)
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                builder.Append(c);
                previousIsDash = false;
            }
            else if (!previousIsDash && builder.Length > 0)
            {
                builder.Append('-');
                previousIsDash = true;
            }
        }

        if (builder.Length > 0 && builder[^1] == '-')
        {
            builder.Length--;
        }

        return builder.ToString();
    }

    public string ApiDotNetRepoToHttpsUrl(string? repo)
    {
        if (repo == null)
        {
            return string.Empty;
        }

        var value = repo.Trim();
        if (value.Length == 0)
        {
            return string.Empty;
        }

        const string gitSshPrefix = "git@github.com:";
        if (value.StartsWith(gitSshPrefix, StringComparison.Ordinal))
        {
            value = $"https://github.com/{value[gitSshPrefix.Length..]}";
        }

        if (value.StartsWith("http://github.com/", StringComparison.Ordinal))
        {
            value = $"https://github.com/{value["http://github.com/".Length..]}";
        }

        value = value.Replace(".git", string.Empty, StringComparison.Ordinal);
        return value;
    }

    public string ApiDotNetSourceUrl(object? source)
    {
        if (source is not ScriptObject sourceObject)
        {
            return string.Empty;
        }

        var remote = sourceObject.GetSafeValue<ScriptObject>("remote");
        if (remote == null)
        {
            return string.Empty;
        }

        var repo = ApiDotNetRepoToHttpsUrl(GetString(remote, "repo"));
        if (repo.Length == 0)
        {
            return string.Empty;
        }

        var branch = GetString(remote, "branch");
        if (branch.Length == 0)
        {
            branch = "main";
        }

        var path = GetString(remote, "path");
        if (path.Length == 0)
        {
            return string.Empty;
        }

        var url = $"{repo}/blob/{branch}/{path}";
        var startLine = GetInteger(sourceObject, "startLine");
        if (startLine > 0)
        {
            url = $"{url}#L{startLine}";
        }

        return url;
    }

    public string ApiDotNetKindIcon(string? kind)
    {
        if (!string.IsNullOrWhiteSpace(kind) && _kindIcons.TryGetValue(kind, out var iconValue))
        {
            return BuildIconMarkup(iconValue);
        }

        return _kindIcons.TryGetValue("default", out var defaultIcon) ? BuildIconMarkup(defaultIcon) : string.Empty;
    }

    public string ApiDotNetMembersRender(ScriptObject? apiObject, string name, string text, string? membersClass = null)
    {
        if (apiObject == null || string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        var members = GetMembers(apiObject, name);
        if (members.Count == 0)
        {
            return string.Empty;
        }

        var headingText = text ?? string.Empty;
        var anchor = ApiDotNetHandleize(headingText);
        var effectiveMembersClass = string.IsNullOrWhiteSpace(membersClass) ? DefaultMembersClass : membersClass;
        var builder = new StringBuilder(512);
        builder.Append("\n\n<h2 id=\"");
        builder.Append(HtmlEscape(anchor));
        builder.Append("\">");
        builder.Append(HtmlEscape(headingText));
        builder.Append(" <span class=\"api-dotnet-count\">(");
        builder.Append(members.Count);
        builder.Append(")</span></h2>\n");
        builder.Append("<div class=\"");
        builder.Append(HtmlEscape(effectiveMembersClass));
        builder.Append("\" data-api-dotnet-members=\"");
        builder.Append(HtmlEscape(anchor));
        builder.Append("\">\n");

        foreach (var member in members)
        {
            var memberKind = GetString(member, "type");
            if (name.Equals("extensions", StringComparison.Ordinal))
            {
                memberKind = "Extension";
            }
            else if (name.Equals("explicit_interface_implementation_methods", StringComparison.Ordinal))
            {
                memberKind = "EiiMethod";
            }

            var uid = GetString(member, "uid");
            var title = GetString(member, "name");
            if (string.IsNullOrWhiteSpace(title))
            {
                title = uid;
            }

            string? linkUrl = null;
            if (uid.Length > 0 && TryResolveXRef(uid, false, out var linkTitle, out var resolvedUrl))
            {
                title = linkTitle;
                linkUrl = resolvedUrl;
            }

            var summary = NormalizeMemberSummary(GetString(member, "summary"));
            var kindAttribute = HtmlEscape(memberKind);
            var iconHtml = ApiDotNetKindIcon(memberKind);

            builder.Append("<div class=\"api-dotnet-member-item list-group-item");
            if (linkUrl != null)
            {
                builder.Append(" api-dotnet-member-item-has-link");
            }
            builder.Append("\" data-api-kind=\"");
            builder.Append(kindAttribute);
            builder.Append("\">");

            if (linkUrl != null)
            {
                builder.Append("<a class=\"api-dotnet-member-title api-dotnet-member-title-link\" href=\"");
                builder.Append(HtmlEscape(linkUrl));
                builder.Append("\">");
                builder.Append(iconHtml);
                builder.Append("<code class=\"api-dotnet-member-name\">");
                builder.Append(HtmlEscape(title));
                builder.Append("</code></a>");
            }
            else
            {
                builder.Append("<span class=\"api-dotnet-member-title\">");
                builder.Append(iconHtml);
                builder.Append("<code class=\"api-dotnet-member-name\">");
                builder.Append(HtmlEscape(title));
                builder.Append("</code></span>");
            }

            if (summary.Length > 0)
            {
                builder.Append("<span class=\"api-dotnet-member-summary\">");
                builder.Append(summary);
                builder.Append("</span>");
            }

            builder.Append("</div>\n");
        }

        builder.Append("</div>\n");
        return builder.ToString();
    }

    private bool TryResolveXRef(string uid, bool useFullName, out string name, out string url)
    {
        var finder = _site.Content.Finder;
        if (finder.TryFindByUid(uid, out var page))
        {
            name = useFullName
                ? page.GetSafeValue<string>(PageVariables.XRefFullName)
                    ?? page.GetSafeValue<string>(PageVariables.XRefName)
                    ?? page.Title
                    ?? uid
                : page.GetSafeValue<string>(PageVariables.XRefName)
                    ?? page.Title
                    ?? uid;

            url = page.Url ?? string.Empty;
            return url.Length > 0;
        }

        if (finder.TryGetExternalUid(uid, out var externalName, out var externalFullName, out var externalUrl) &&
            !string.IsNullOrEmpty(externalUrl))
        {
            name = useFullName ? externalFullName ?? externalName ?? uid : externalName ?? uid;
            url = externalUrl;
            return true;
        }

        name = uid;
        url = string.Empty;
        return false;
    }

    private static List<ScriptObject> GetMembers(ScriptObject apiObject, string name)
    {
        if (!apiObject.TryGetValue(name, out var membersObject) || membersObject == null)
        {
            return [];
        }

        if (membersObject is IEnumerable<ScriptObject> typedMembers)
        {
            return typedMembers.ToList();
        }

        if (membersObject is not IEnumerable members)
        {
            return [];
        }

        var result = new List<ScriptObject>();
        foreach (var item in members)
        {
            if (item is ScriptObject scriptObject)
            {
                result.Add(scriptObject);
            }
        }

        return result;
    }

    private static string NormalizeMemberSummaryText(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return string.Empty;
        }

        var value = summary.Trim()
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Replace('\r', ' ');

        return value;
    }

    private string NormalizeMemberSummary(string summary)
    {
        var value = NormalizeMemberSummaryText(summary);
        if (value.Length == 0)
        {
            return string.Empty;
        }

        value = ApiDotNetResolveXrefs(value);
        if (value.StartsWith("<p>", StringComparison.Ordinal) &&
            value.EndsWith("</p>", StringComparison.Ordinal) &&
            value.Length >= 7)
        {
            value = value[3..^4].Trim();
        }

        value = value.Replace("<p>", string.Empty, StringComparison.Ordinal)
            .Replace("</p>", " ", StringComparison.Ordinal)
            .Trim();

        return value;
    }

    private static string GetString(ScriptObject obj, string key)
    {
        if (!obj.TryGetValue(key, out var value) || value == null)
        {
            return string.Empty;
        }

        return value as string ?? value.ToString() ?? string.Empty;
    }

    private static int GetInteger(ScriptObject obj, string key)
    {
        if (!obj.TryGetValue(key, out var value) || value == null)
        {
            return 0;
        }

        return value switch
        {
            byte byteValue => byteValue,
            sbyte sbyteValue => sbyteValue,
            short shortValue => shortValue,
            ushort ushortValue => ushortValue,
            int intValue => intValue,
            uint uintValue => unchecked((int)uintValue),
            long longValue => unchecked((int)longValue),
            ulong ulongValue => unchecked((int)ulongValue),
            float floatValue => (int)floatValue,
            double doubleValue => (int)doubleValue,
            decimal decimalValue => (int)decimalValue,
            string stringValue when int.TryParse(stringValue, out var parsed) => parsed,
            _ => 0
        };
    }

    private static string HtmlEscape(string? text)
    {
        return WebUtility.HtmlEncode(text ?? string.Empty);
    }

    private static Dictionary<string, string> BuildKindIcons(ScriptObject? kindIconsConfig)
    {
        var icons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["default"] = "bi-file-earmark-code",
            ["Namespace"] = "bi-diagram-3",
            ["Class"] = "bi-box",
            ["Struct"] = "bi-boxes",
            ["Interface"] = "bi-diagram-3",
            ["Enum"] = "bi-list-ul",
            ["Delegate"] = "bi-code-slash",
            ["Constructor"] = "bi-hammer",
            ["Field"] = "bi-hash",
            ["Property"] = "bi-sliders",
            ["Method"] = "bi-gear",
            ["Event"] = "bi-bell",
            ["Operator"] = "bi-calculator",
            ["Extension"] = "bi-plugin",
            ["EiiMethod"] = "bi-link-45deg",
            ["Api"] = "bi-braces-asterisk",
        };

        if (kindIconsConfig == null)
        {
            return icons;
        }

        foreach (var (key, value) in kindIconsConfig)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            icons[key] = value?.ToString()?.Trim() ?? string.Empty;
        }

        return icons;
    }

    private static string BuildIconMarkup(string? iconValue)
    {
        if (string.IsNullOrWhiteSpace(iconValue))
        {
            return string.Empty;
        }

        var value = iconValue.Trim();
        if (value.StartsWith('<'))
        {
            return value;
        }

        if (value.StartsWith("bi-", StringComparison.Ordinal))
        {
            value = $"bi {value}";
        }

        if (!value.Contains("api-dotnet-kind-icon", StringComparison.Ordinal))
        {
            value = $"{value} api-dotnet-kind-icon";
        }

        return $"<i class=\"{HtmlEscape(value)}\" aria-hidden=\"true\"></i>";
    }
}
