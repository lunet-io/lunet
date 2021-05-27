// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Linq;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    public class SetDerivedClass: IResolverPipeline
    {
        private readonly Dictionary<string, HashSet<string>> _derivedClassMapping = new Dictionary<string, HashSet<string>>();

        public void Run(MetadataModel yaml, ResolverContext context)
        {
            if (yaml.Members != null && yaml.Members.Count > 0)
            {
                UpdateDerivedClassMapping(yaml.Members, context.References);
                AppendDerivedClass(yaml.Members);
            }
        }

        private void UpdateDerivedClassMapping(List<MetadataItem> items, Dictionary<string, ReferenceItem> reference)
        {
            foreach (var item in items ?? Enumerable.Empty<MetadataItem>())
            {
                var inheritance = item.Inheritance;
                if (inheritance != null && inheritance.Count > 0)
                {
                    var superClass = inheritance[inheritance.Count - 1];
                    AddInheritance(item, superClass, reference);
                }

                var implements = item.Implements;
                if (implements != null && implements.Count > 0)
                {
                    foreach (var implement in implements)
                    {
                        AddInheritance(item, implement, reference);
                    }
                }
            }
        }

        private void AddInheritance(MetadataItem item, string superClass, Dictionary<string, ReferenceItem> reference)
        {
            if (reference.TryGetValue(superClass, out ReferenceItem referenceItem))
            {
                superClass = referenceItem.Definition ?? superClass;
            }

            // ignore System.Object's derived class
            if (superClass != "System.Object")
            {
                if (_derivedClassMapping.TryGetValue(superClass, out HashSet<string> derivedClasses))
                {
                    derivedClasses.Add(item.Name);
                }
                else
                {
                    _derivedClassMapping.Add(superClass, new HashSet<string>() { item.Name });
                }
            }
        }

        private void AppendDerivedClass(List<MetadataItem> items)
        {
            foreach (var item in items ?? Enumerable.Empty<MetadataItem>())
            {
                if (item.Type == MemberType.Class || item.Type == MemberType.Struct || item.Type == MemberType.Interface)
                {
                    if (_derivedClassMapping.TryGetValue(item.Name, out var derivedClasses))
                    {
                        var derivedClassesList = derivedClasses.ToList();
                        derivedClassesList.Sort();
                        item.DerivedClasses = derivedClassesList;
                    }
                }
            }
        }
    }
}