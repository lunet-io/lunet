// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using Microsoft.Extensions.Logging;
using Scriban.Parsing;

namespace Lunet.Core
{
    public interface ISiteLoggerProvider
    {
        SiteLoggerFactory LoggerFactory { get; }

        public int LogEventId { get; set; }

        ILogger Log { get; }
    }

    /// <summary>
    /// Extensions for <see cref="SiteObject"/>
    /// </summary>
    public static class SiteObjectExtensions
    {
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
            return site.Log.IsEnabled(LogLevel.Information);
        }

        public static void Info(this ISiteLoggerProvider site, string message, params object[] args)
        {
            site.Log.LogInformation(new EventId(site.LogEventId++), message, args);
        }

        public static void Error(this ISiteLoggerProvider site, string message, params object[] args)
        {
            site.Log.LogError(new EventId(site.LogEventId++), message, args);
        }
        
        public static void Error(this ISiteLoggerProvider site, Exception exception, string message, params object[] args)
        {
            site.Log.LogError(new EventId(site.LogEventId++), exception,  message, args);
        }

        public static void Warning(this ISiteLoggerProvider site, string message, params object[] args)
        {
            site.Log.LogWarning(new EventId(site.LogEventId++), message, args);
        }

        public static void Fatal(this ISiteLoggerProvider site, string message, params object[] args)
        {
            site.Log.LogCritical(new EventId(site.LogEventId++), message, args);
        }

        public static void Trace(this ISiteLoggerProvider site, string message, params object[] args)
        {
            site.Log.LogTrace(new EventId(site.LogEventId++), message, args);
        }

        public static void Debug(this ISiteLoggerProvider site, string message, params object[] args)
        {
            site.Log.LogDebug(new EventId(site.LogEventId++), message, args);
        }

        public static void Info(this ISiteLoggerProvider site, SourceSpan span, string message, params object[] args)
        {
            site.Log.LogInformation(new EventId(site.LogEventId++), GetSpanMessage(site, span, message), args);
        }

        public static void Error(this ISiteLoggerProvider site, SourceSpan span, string message, params object[] args)
        {
            site.Log.LogError(new EventId(site.LogEventId++), GetSpanMessage(site, span, message), args);
        }

        public static void Warning(this ISiteLoggerProvider site, SourceSpan span, string message, params object[] args)
        {
            site.Log.LogWarning(new EventId(site.LogEventId++), GetSpanMessage(site, span, message), args);
        }

        public static void Fatal(this ISiteLoggerProvider site, SourceSpan span, string message, params object[] args)
        {
            site.Log.LogCritical(new EventId(site.LogEventId++), GetSpanMessage(site, span, message), args);
        }

        public static void Trace(this ISiteLoggerProvider site, SourceSpan span, string message, params object[] args)
        {
            site.Log.LogTrace(new EventId(site.LogEventId++), GetSpanMessage(site, span, message), args);
        }

        public static void Debug(this ISiteLoggerProvider site, SourceSpan span, string message, params object[] args)
        {
            site.Log.LogDebug(new EventId(site.LogEventId++), GetSpanMessage(site, span, message), args);
        }

        private static string GetSpanMessage(ISiteLoggerProvider site, SourceSpan span, string message)
        {
            var fileRelative = span.FileName ?? string.Empty;
            return $"In {fileRelative}({span.Start.ToStringSimple()}): {message}";
        }
    }
}