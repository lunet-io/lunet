// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using Lunet.Helpers;
using Microsoft.Extensions.Logging;
using Textamina.Scriban.Parsing;

namespace Lunet.Runtime
{
    /// <summary>
    /// Extensions for <see cref="SiteObject"/>
    /// </summary>
    public static class SiteObjectExtensions
    {
        /// <summary>
        /// Gets a directory relative to the base directory of this site.
        /// </summary>
        /// <param name="site">The site.</param>
        /// <param name="subDirectoryPath">The sub directory path.</param>
        /// <returns>
        /// The relative directory
        /// </returns>
        /// <exception cref="System.ArgumentNullException">if <paramref name="subDirectoryPath" /> is null</exception>
        /// <exception cref="LunetException">If <paramref name="subDirectoryPath" /> is using `..` and going above the base directory.</exception>
        public static DirectoryInfo GetSubDirectory(this SiteObject site, string subDirectoryPath)
        {
            if (subDirectoryPath == null) throw new ArgumentNullException(nameof(subDirectoryPath));
            return PathUtil.GetSubDirectory(site.BaseDirectory, subDirectoryPath);
        }

        /// <summary>
        /// Gets a relative path to the site base directory from the specified absolute path.
        /// </summary>
        /// <param name="site">The site.</param>
        /// <param name="fullFilePath">The full file path.</param>
        /// <param name="normalized"><c>true</c> to return a normalize path using only '/' for directory separators</param>
        /// <returns>A relative path</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="LunetException"></exception>
        public static string GetRelativePath(this SiteObject site, string fullFilePath, bool normalized = false)
        {
            return PathUtil.GetRelativePath(site.BaseDirectory, fullFilePath, normalized);
        }

        public static bool CanTrace(this SiteObject site)
        {
            return site.Log.IsEnabled(LogLevel.Trace);
        }

        public static bool CanDebug(this SiteObject site)
        {
            return site.Log.IsEnabled(LogLevel.Debug);
        }

        public static void Info(this SiteObject site, string message, params object[] args)
        {
            site.Log.LogInformation(message, args);
        }

        public static void Error(this SiteObject site, string message, params object[] args)
        {
            site.Log.LogError(message, args);
        }

        public static void Warning(this SiteObject site, string message, params object[] args)
        {
            site.Log.LogWarning(message, args);
        }

        public static void Critical(this SiteObject site, string message, params object[] args)
        {
            site.Log.LogCritical(message, args);
        }

        public static void Trace(this SiteObject site, string message, params object[] args)
        {
            site.Log.LogTrace(message, args);
        }

        public static void Debug(this SiteObject site, string message, params object[] args)
        {
            site.Log.LogDebug(message, args);
        }

        public static void Info(this SiteObject site, SourceSpan span, string message, params object[] args)
        {
            site.Log.LogInformation(GetSpanMessage(site, span, message), args);
        }

        public static void Error(this SiteObject site, SourceSpan span, string message, params object[] args)
        {
            site.Log.LogError(GetSpanMessage(site, span, message), args);
        }

        public static void Warning(this SiteObject site, SourceSpan span, string message, params object[] args)
        {
            site.Log.LogWarning(GetSpanMessage(site, span, message), args);
        }

        public static void Critical(this SiteObject site, SourceSpan span, string message, params object[] args)
        {
            site.Log.LogCritical(GetSpanMessage(site, span, message), args);
        }

        public static void Trace(this SiteObject site, SourceSpan span, string message, params object[] args)
        {
            site.Log.LogTrace(GetSpanMessage(site, span, message), args);
        }

        public static void Debug(this SiteObject site, SourceSpan span, string message, params object[] args)
        {
            site.Log.LogDebug(GetSpanMessage(site, span, message), args);
        }

        private static string GetSpanMessage(SiteObject site, SourceSpan span, string message)
        {
            var fileRelative = span.FileName != null
                ? site.GetRelativePath(span.FileName)
                : string.Empty;
            return $"In {fileRelative}({span.Start.ToStringSimple()}): {message}";
        }
    }
}