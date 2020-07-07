﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using Lunet.Core;
using Scriban.Runtime;

namespace Lunet.Bundles
{
    /// <summary>
    /// Manages resources.
    /// </summary>
    public class BundlePlugin : SitePlugin
    {
        private delegate IDynamicObject BundleFunctionDelegate(params object[] args);

        public const string DefaultBundleName = "site";

        public BundlePlugin(SiteObject site) : base(site)
        {
            List = new List<BundleObject>();

            Site.SetValue(SiteVariables.Bundles, List, true);

            // Add the bundle builtins to be included by default in site.html.head_includes
            Site.Html.Head.Includes.Add("builtins/bundle.sbn-html");

            // The "bundle" function is global as it is used inside scripts and inside
            Site.Scripts.Builtins.Import(GlobalVariables.BundleFunction, (BundleFunctionDelegate)BundleFunction);

            BundleProcessor = new BundleProcessor(this);

            Site.Content.BeforeProcessingProcessors.Add(BundleProcessor);
        }

        public BundleProcessor BundleProcessor { get; }

        public List<BundleObject> List { get; }

        public BundleObject FindBundle(string bundleName)
        {
            bundleName = GetDefaultBundleName(bundleName);
            foreach (var bundle in List)
            {
                if (bundle.Name == bundleName)
                {
                    return bundle;
                }
            }
            return null;
        }

        public BundleObject GetOrCreateBundle(string bundleName)
        {
            bundleName = GetDefaultBundleName(bundleName);
            var bundle = FindBundle(bundleName);
            if (bundle != null)
            {
                return bundle;
            }

            bundle = new BundleObject(this, bundleName);
            List.Add(bundle);

            return bundle;
        }

        private string GetDefaultBundleName(string bundleName)
        {
            var newBundleName = bundleName;
            if (string.IsNullOrWhiteSpace(bundleName))
            {
                newBundleName = DefaultBundleName;
            }
            return newBundleName;
        }

        /// <summary>
        /// The `bundle` function accessible from scripts.
        /// </summary>
        /// <param name="args"></param>
        /// <returns>A ScriptObject </returns>
        /// <exception cref="LunetException">Unsupported resource parameter found. Supports either a plain string or an object with at least the properties { name: \providerName/packageName[/packageVersion]\ }</exception>
        private BundleObject BundleFunction(params object[] args)
        {
            if (args.Length > 1)
            {
                throw new ArgumentException("Expecting zero or one argument for `bundle \"<bundleName>\"?` function");
            }
            var bundle = GetOrCreateBundle(args.Length == 1 ? args[0] as string : null);
            return bundle;
        }
    }
}