// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using Lunet.Scripts;
using Scriban.Runtime;
using Scriban.Syntax;
using Zio;

namespace Lunet.Core
{
    public abstract class TemplateObject : DynamicObject
    {
        protected TemplateObject(SiteObject site, ContentObjectType objectType, FileEntry sourceFileInfo = null, ScriptInstance scriptInstance = null, UPath? path = null)
        {
            Site = site ?? throw new ArgumentNullException(nameof(site));
            SourceFile = sourceFileInfo;
            FrontMatter = scriptInstance?.FrontMatter;
            Script = scriptInstance?.Template;
            ObjectType = objectType;
            Dependencies = new List<ContentDependency>();


            Path = path ?? SourceFile?.Path ?? null;
            if (SourceFile != null)
            {
                Length = SourceFile.Length;
                Extension = SourceFile.ExtensionWithDot?.ToLowerInvariant();
                ModifiedTime = SourceFile.LastWriteTime;
            }
        }

        public SiteObject Site { get; }

        public FileEntry SourceFile { get; }

        public long Length { get; }

        public ContentObjectType ObjectType { get; }

        public IFrontMatter FrontMatter { get; set; }

        public UPath Path { get; }

        public DateTime ModifiedTime { get; }

        public string Extension { get; }

        /// <summary>
        /// Gets or sets the script attached to this page if any.
        /// </summary>
        public ScriptPage Script { get; }

        public ScriptObject ScriptObjectLocal { get; set; }

        public bool HasFrontMatter => FrontMatter != null;

        public List<ContentDependency> Dependencies { get; }
    }
}