// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json; using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;

    using Microsoft.CodeAnalysis;

    public class ExtractMetadataOptions
    {
        public bool ShouldSkipMarkup { get; set; }

        public bool PreserveRawInlineComments { get; set; }

        public string FilterConfigFile { get; set; }

        public Dictionary<string, string> MSBuildProperties { get; set; }

        public string CodeSourceBasePath { get; set; }

        public bool DisableDefaultFilter { get; set; }

        [JsonIgnore]
        public IReadOnlyDictionary<Compilation, IEnumerable<IMethodSymbol>> RoslynExtensionMethods { get; set; }
    }
}
