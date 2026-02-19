// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Lunet.Core;
using Lunet.Helpers;

namespace Lunet.Api.DotNet;

public static class ApiDotNetSlugGenerator
{
    public const int DefaultMaxLength = 96;
    public const int MinMaxLength = 24;
    public const int MaxAllowedLength = 240;
    private const int HashLength = 12;

    private static readonly HashSet<string> ReservedWindowsNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "con", "prn", "aux", "nul",
        "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
        "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9",
    };

    public static int NormalizeMaxLength(int requestedLength)
    {
        var length = requestedLength <= 0 ? DefaultMaxLength : requestedLength;
        if (length < MinMaxLength) length = MinMaxLength;
        if (length > MaxAllowedLength) length = MaxAllowedLength;
        return length;
    }

    public static string BuildSlug(string uid, int maxLength, int disambiguator = 0)
    {
        ArgumentNullException.ThrowIfNull(uid);

        var normalizedMaxLength = NormalizeMaxLength(maxLength);
        var directSlug = UidHelper.Handleize(uid);
        if (directSlug.Length == 0)
        {
            directSlug = "api";
        }

        var forceHashedSlug = disambiguator > 0
                              || directSlug.Length > normalizedMaxLength
                              || !IsSafeDirectSlug(directSlug);
        if (!forceHashedSlug)
        {
            return directSlug;
        }

        var uidSlug = NormalizeForFileSegment(directSlug);
        var hashInput = disambiguator == 0 ? uid : $"{uid}#{disambiguator}";
        var hashLength = Math.Min(HashLength, normalizedMaxLength - 2);
        var hash = HashUtil.HashStringHex(hashInput).Substring(0, hashLength);

        var prefixLength = normalizedMaxLength - hashLength - 1;
        var prefix = prefixLength > 0
            ? NormalizeForFileSegment(uidSlug.Length > prefixLength ? uidSlug.Substring(0, prefixLength) : uidSlug)
            : string.Empty;

        string slug;
        if (prefix.Length == 0)
        {
            slug = hash;
        }
        else
        {
            slug = $"{prefix}-{hash}";
        }

        slug = NormalizeForFileSegment(slug);
        if (slug.Length == 0)
        {
            slug = $"api-{hash}";
        }

        if (slug.Length > normalizedMaxLength)
        {
            slug = slug.Substring(0, normalizedMaxLength);
            slug = NormalizeForFileSegment(slug);
        }

        if (IsReservedWindowsName(slug))
        {
            var reservedPrefixLength = Math.Max(1, normalizedMaxLength - hashLength - 2);
            var reservedPrefix = slug.Length > reservedPrefixLength ? slug.Substring(0, reservedPrefixLength) : slug;
            reservedPrefix = NormalizeForFileSegment(reservedPrefix);
            slug = $"{reservedPrefix}-x-{hash}";
            slug = NormalizeForFileSegment(slug);
            if (slug.Length > normalizedMaxLength)
            {
                slug = slug.Substring(0, normalizedMaxLength);
                slug = NormalizeForFileSegment(slug);
            }
        }

        return slug.Length == 0 ? "api" : slug;
    }

    private static bool IsSafeDirectSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value == "." || value == "..")
        {
            return false;
        }

        if (value.EndsWith(".", StringComparison.Ordinal) || value.EndsWith(" ", StringComparison.Ordinal))
        {
            return false;
        }

        return !IsReservedWindowsName(value);
    }

    private static bool IsReservedWindowsName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.TrimEnd(' ', '.');
        var dotIndex = normalized.IndexOf('.');
        if (dotIndex >= 0)
        {
            normalized = normalized.Substring(0, dotIndex);
        }

        return ReservedWindowsNames.Contains(normalized);
    }

    private static string NormalizeForFileSegment(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var previousDash = false;
        foreach (var c in value)
        {
            if (c == '-')
            {
                if (previousDash)
                {
                    continue;
                }
                previousDash = true;
                builder.Append(c);
                continue;
            }

            previousDash = false;
            builder.Append(c);
        }

        var normalized = builder.ToString().Trim(' ', '.', '-');
        if (normalized == "." || normalized == "..")
        {
            return string.Empty;
        }

        return normalized;
    }
}

public sealed class ApiDotNetSlugResolver
{
    private readonly int _maxLength;
    private readonly Dictionary<string, string> _uidToSlug;
    private readonly HashSet<string> _allocatedSlugs;

    public ApiDotNetSlugResolver(int maxLength)
    {
        _maxLength = ApiDotNetSlugGenerator.NormalizeMaxLength(maxLength);
        _uidToSlug = new Dictionary<string, string>(StringComparer.Ordinal);
        _allocatedSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public string GetSlug(string uid)
    {
        ArgumentNullException.ThrowIfNull(uid);

        if (_uidToSlug.TryGetValue(uid, out var existingSlug))
        {
            return existingSlug;
        }

        var disambiguator = 0;
        while (true)
        {
            var slug = ApiDotNetSlugGenerator.BuildSlug(uid, _maxLength, disambiguator);
            if (_allocatedSlugs.Add(slug))
            {
                _uidToSlug.Add(uid, slug);
                return slug;
            }

            disambiguator++;
        }
    }
}
