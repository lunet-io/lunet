// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    public static class YamlMetadataResolver
    {
        // Order matters
        // CHANGED by xoofx: Rebuild the pipelines because it is not thread-safe!
        private static List<IResolverPipeline> CreatePipelines() => new List<IResolverPipeline>
        {
            new LayoutCheckAndCleanup(),
            new SetParent(),
            new CopyInherited(),
            new ResolveReference(),
            new NormalizeSyntax(),
            new BuildMembers(),
            new SetDerivedClass(),
            new BuildToc()
        };

        /// <summary>
        /// TODO: input Namespace list instead; 
        /// TODO: Save to ...yml.map
        /// </summary>
        /// <param name="allMembers"></param>
        /// <returns></returns>
        public static MetadataModel ResolveMetadata(
            Dictionary<string, MetadataItem> allMembers,
            Dictionary<string, ReferenceItem> allReferences,
            bool preserveRawInlineComments)
        {
            MetadataModel viewModel = new MetadataModel();
            viewModel.TocYamlViewModel = new MetadataItem
            {
                Type = MemberType.Toc,
                Items = allMembers.Where(s => s.Value.Type == MemberType.Namespace).Select(s => s.Value).ToList(),
            };
            viewModel.Members = new List<MetadataItem>();
            ResolverContext context = new ResolverContext
            {
                References = allReferences,
                Members = allMembers,
                PreserveRawInlineComments = preserveRawInlineComments,
            };

            ExecutePipeline(viewModel, context);

            return viewModel;
        }

        static void ExecutePipeline(MetadataModel yaml, ResolverContext context)
        {
            foreach (var pipeline in CreatePipelines())
            {
                pipeline.Run(yaml, context);
            }
        }
    }
}
