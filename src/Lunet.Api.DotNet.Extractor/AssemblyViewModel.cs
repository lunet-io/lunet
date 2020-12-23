// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.DocAsCode.DataContracts.Common;
using Microsoft.DocAsCode.DataContracts.ManagedReference;
using Newtonsoft.Json;

namespace Lunet.Api.DotNet.Extractor
{
    public class AssemblyViewModel
    {
        [JsonProperty("name")]
        public string Name { get; set; }


        [JsonProperty("items")]
        public List<PageViewModel> Items { get; set; }
    }
}