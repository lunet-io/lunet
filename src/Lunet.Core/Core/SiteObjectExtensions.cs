// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Globalization;
using Lunet.Helpers;
using Scriban.Parsing;
using XenoAtom.Logging;

namespace Lunet.Core;

public interface ISiteLoggerProvider
{
    SiteLoggerFactory LoggerFactory { get; }

    int LogEventId { get; set; }

    SiteLogger Log { get; }
        
    bool ShowStacktraceOnError { get; set; }
}

/// <summary>
/// Extensions for <see cref="SiteObject"/>
/// </summary>
public static class SiteObjectExtensions
{
    public static void InsertBefore<T>(this OrderedList<T> list, string name, T value) where T: ISitePluginCore
    {
        for (var i = 0; i < list.Count; i++)
        {
            var item = list[i];
            if (item.Name == name)
            {
                list.Insert(i, value);
                return;
            }
        }
        list.Add(value);
    }

    public static void BeginEvent(this SiteObject site, string name)
    {
        BeginEvent(site, name, ProfilerColor.Default);
    }

    public static void BeginEvent(this SiteObject site, string name, ProfilerColor color)
    {
        var profiler = site.Config.Profiler;
        if (profiler == null) return;
        profiler.BeginEvent(name, null, color);
    }

    public static void EndEvent(this SiteObject site)
    {
        var profiler = site.Config.Profiler;
        if (profiler == null) return;
        profiler.EndEvent();
    }

    public static bool CanTrace(this ISiteLoggerProvider site)
    {
        return site.Log.IsEnabled(LogLevel.Trace);
    }

    public static bool CanDebug(this ISiteLoggerProvider site)
    {
        return site.Log.IsEnabled(LogLevel.Debug);
    }

    public static bool CanInfo(this ISiteLoggerProvider site)
    {
        return site.Log.IsEnabled(LogLevel.Info);
    }

    public static void Info(this ISiteLoggerProvider site, string message, params object[] args)
    {
        site.Log.Log(LogLevel.Info, NewEventId(site), FormatMessage(message, args));
    }

    public static void Error(this ISiteLoggerProvider site, string message, params object[] args)
    {
        site.Log.Log(LogLevel.Error, NewEventId(site), FormatMessage(message, args));
    }
        
    public static void Error(this ISiteLoggerProvider site, Exception exception, string message, params object[] args)
    {
        site.Log.Log(LogLevel.Error, NewEventId(site), FormatMessage(message, args));
        if (site.ShowStacktraceOnError && exception is not null)
        {
            site.Log.Log(LogLevel.Error, NewEventId(site), exception.ToString());
        }
    }

    public static void Warning(this ISiteLoggerProvider site, string message, params object[] args)
    {
        site.Log.Log(LogLevel.Warn, NewEventId(site), FormatMessage(message, args));
    }

    public static void Fatal(this ISiteLoggerProvider site, string message, params object[] args)
    {
        site.Log.Log(LogLevel.Fatal, NewEventId(site), FormatMessage(message, args));
    }

    public static void Trace(this ISiteLoggerProvider site, string message, params object[] args)
    {
        site.Log.Log(LogLevel.Trace, NewEventId(site), FormatMessage(message, args));
    }

    public static void Debug(this ISiteLoggerProvider site, string message, params object[] args)
    {
        site.Log.Log(LogLevel.Debug, NewEventId(site), FormatMessage(message, args));
    }

    public static void Info(this ISiteLoggerProvider site, SourceSpan span, string message, params object[] args)
    {
        site.Log.Log(LogLevel.Info, NewEventId(site), FormatMessage(GetSpanMessage(site, span, message), args));
    }

    public static void Error(this ISiteLoggerProvider site, SourceSpan span, string message, params object[] args)
    {
        site.Log.Log(LogLevel.Error, NewEventId(site), FormatMessage(GetSpanMessage(site, span, message), args));
    }

    public static void Warning(this ISiteLoggerProvider site, SourceSpan span, string message, params object[] args)
    {
        site.Log.Log(LogLevel.Warn, NewEventId(site), FormatMessage(GetSpanMessage(site, span, message), args));
    }

    public static void Fatal(this ISiteLoggerProvider site, SourceSpan span, string message, params object[] args)
    {
        site.Log.Log(LogLevel.Fatal, NewEventId(site), FormatMessage(GetSpanMessage(site, span, message), args));
    }

    public static void Trace(this ISiteLoggerProvider site, SourceSpan span, string message, params object[] args)
    {
        site.Log.Log(LogLevel.Trace, NewEventId(site), FormatMessage(GetSpanMessage(site, span, message), args));
    }

    public static void Debug(this ISiteLoggerProvider site, SourceSpan span, string message, params object[] args)
    {
        site.Log.Log(LogLevel.Debug, NewEventId(site), FormatMessage(GetSpanMessage(site, span, message), args));
    }

    private static string GetSpanMessage(ISiteLoggerProvider site, SourceSpan span, string message)
    {
        var fileRelative = span.FileName ?? string.Empty;
        return $"In {fileRelative}({span.Start.ToStringSimple()}): {message}";
    }

    private static string FormatMessage(string message, object[] args)
    {
        if (args is null || args.Length == 0)
        {
            return message;
        }

        return string.Format(CultureInfo.InvariantCulture, message, args);
    }

    private static LogEventId NewEventId(ISiteLoggerProvider site)
    {
        return new LogEventId(site.LogEventId++, null);
    }
}
