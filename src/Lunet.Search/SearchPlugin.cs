// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;
using Scriban.Runtime;
using Zio;

namespace Lunet.Search
{
    public class SearchPlugin : SitePlugin
    {
        public static readonly UPath DefaultUrl = UPath.Root / "js" / "search.db";

        public SearchPlugin(SiteObject site) : base(site)
        {
            Enable = false;
            Url = (string)DefaultUrl;

            Excludes = new ScriptArray();
            Excludes.ScriptObject.Import("add", (Action<object>)AddExclude);
            SetValue("excludes", Excludes, true);

            site.SetValue("search", this, true);
            var processor = new SearchProcessor(this);
            site.Content.BeforeLoadingProcessors.Add(processor);
            site.Content.BeforeProcessingProcessors.Add(processor);
            site.Content.AfterLoadingProcessors.Add(processor);
        }

        public bool Enable
        {
            get => GetSafeValue<bool>("enable");
            set => SetValue("enable", value);
        }

        public ScriptArray Excludes { get; }

        public string Url
        {
            get => GetSafeValue<string>("url");
            set => SetValue("url", value);
        }

        private void AddExclude(object item)
        {
            if (item == null) return;

            if (item is string str)
            {
                if (!UPath.TryParse(str, out var path))
                {
                    throw new ArgumentException($"Invalid path `{str}`. The path is malformed.", nameof(item));
                }

                if (!path.IsAbsolute)
                {
                    throw new ArgumentException($"Invalid path `{str}`. Expecting an absolute path.", nameof(item));
                }

                Excludes.Add(item);
            }
            else if (item is ScriptArray array)
            {
                foreach (var itemToAdd in array)
                {
                    AddExclude(itemToAdd);
                }
            }
            else
            {
                throw new ArgumentException($"Invalid path. Expecting a string instead of `{item.GetType().FullName}`.", nameof(item));
            }
        }
    }
}