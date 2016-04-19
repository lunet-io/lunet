// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using Lunet.Core;
using Microsoft.Extensions.Logging;

namespace Lunet
{
    /// <summary>
    /// 
    /// </summary>
    public static class SiteFactory
    {
        /// <summary>
        /// The default configuration filename config.sban
        /// </summary>
        public const string DefaultConfigFilename = "config.sban";

        /// <summary>
        /// Gets the <see cref="SiteObject"/> from the specified configuration file path.
        /// </summary>
        /// <param name="configFilePath">The configuration file path.</param>
        /// <returns>The <see cref="SiteObject"/></returns>
        /// <exception cref="System.IO.FileNotFoundException">If the <paramref name="configFilePath"/> file does not exist.</exception>
        public static SiteObject FromFile(string configFilePath)
        {
            return FromFile(configFilePath, null);
        }

        /// <summary>
        /// Gets the <see cref="SiteObject"/> from the specified configuration file path.
        /// </summary>
        /// <param name="configFilePath">The configuration file path.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <returns>The <see cref="SiteObject"/></returns>
        /// <exception cref="System.IO.FileNotFoundException">If the <paramref name="configFilePath"/> file does not exist.</exception>
        public static SiteObject FromFile(string configFilePath, ILoggerFactory loggerFactory)
        {
            var site = TryFromFile(configFilePath, loggerFactory);
            if (site == null)
            {
                throw new FileNotFoundException($"The config file [{configFilePath}] is not a valid path", configFilePath);
            }
            return site;
        }

        /// <summary>
        /// Gets the <see cref="SiteObject"/> from the specified directory or any parent directories.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <returns>The <see cref="SiteObject"/></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.IO.FileNotFoundException"></exception>
        public static SiteObject FromDirectory(string directoryPath, ILoggerFactory loggerFactory = null)
        {
            if (directoryPath == null) throw new ArgumentNullException(nameof(directoryPath));
            var directory = new DirectoryInfo(directoryPath);
            while (directory != null)
            {
                var site = TryFromFile(Path.Combine(directory.FullName, SiteFactory.DefaultConfigFilename), loggerFactory);

                if (site != null)
                {
                    return site;
                }

                directory = directory.Parent;
            }

            return null;
        }

        private static SiteObject TryFromFile(string configFilePath, ILoggerFactory loggerFactory)
        {
            if (configFilePath == null) throw new ArgumentNullException(nameof(configFilePath));
            if (!File.Exists(configFilePath))
            {
                return null;
            }
            var site = new SiteObject(configFilePath, loggerFactory);
            return site;
        }
    }
}