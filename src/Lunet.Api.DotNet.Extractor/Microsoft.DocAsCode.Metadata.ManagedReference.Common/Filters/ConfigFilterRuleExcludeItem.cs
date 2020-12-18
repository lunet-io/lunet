// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json; using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;

    [Serializable]
    public class ConfigFilterRuleExcludeItem : ConfigFilterRuleItem
    {
        [YamlIgnore]
        public override bool CanVisit
        {
            get
            {
                return false;
            }
        }
    }
}
