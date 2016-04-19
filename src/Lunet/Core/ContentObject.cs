using System;
using System.Diagnostics;
using System.IO;
using Scriban.Helpers;
using Scriban.Runtime;

namespace Lunet.Core
{
    [DebuggerDisplay("Content: {Path}")]
    public class ContentObject : LunetObject
    {
        public ContentObject(DirectoryInfo rootDirectoryInfo, FileInfo sourceFileInfo, SiteObject site)
        {
            if (rootDirectoryInfo == null) throw new ArgumentNullException(nameof(rootDirectoryInfo));
            if (sourceFileInfo == null) throw new ArgumentNullException(nameof(sourceFileInfo));
            if (site == null) throw new ArgumentNullException(nameof(site));
            RootDirectory = rootDirectoryInfo;
            SourceFileInfo = sourceFileInfo;
            SourceFile = sourceFileInfo.FullName;
            Site = site;

            Path = RootDirectory.GetRelativePath(SourceFile, PathFlags.Normalize);
            Length = SourceFileInfo.Length;
            Extension = SourceFileInfo.Extension.ToLowerInvariant();
            ModifiedTime = SourceFileInfo.LastWriteTime;
            ContentExtension = Extension;

            // Extract the section of this content
            var sectionIndex = Path.IndexOf('/');
            if (sectionIndex >= 0)
            {
                Layout = Path.Substring(0, sectionIndex);
                PathInSection = Path.Substring(sectionIndex + 1);
            }
            else
            {
                Layout = string.Empty;
                PathInSection = Path;
            }
            Section = Layout;

            // Extract the default Url of this content
            Url = Path.Substring(0, Path.Length - Extension.Length);

            if (Site.UrlAsFile)
            {
                UrlExplicit = true;
            }

            // Replicate readonly values to the Scripting object
            DynamicObject.SetValue(FileVariables.Length, Length, true);
            DynamicObject.SetValue(FileVariables.ModifiedTime, ModifiedTime, true);
            DynamicObject.SetValue(FileVariables.Path, Path, true);
            DynamicObject.SetValue(FileVariables.Extension, Extension, true);

            DynamicObject.SetValue(PageVariables.Section, Layout, true);
            DynamicObject.SetValue(PageVariables.PathInSection, PathInSection, true);
        }

        public FolderInfo RootDirectory { get; }

        public SiteObject Site { get; }

        public FileInfo SourceFileInfo { get; }

        public string SourceFile { get; }

        public long Length { get; }

        public string Path { get; }

        public ScriptDate ModifiedTime { get; }

        public string Extension { get; }

        public string Section { get; }

        public string PathInSection { get; }


        public bool Discard
        {
            get { return DynamicObject.GetSafeValue<bool>(FileVariables.Discard); }
            set { DynamicObject[FileVariables.Discard] = value; }
        }
       

        /// <summary>
        /// Gets or sets the script attached to this page if any.
        /// </summary>
        public ScriptPage Script { get; set; }

        public ScriptObject ScriptObjectLocal { get; set; }

        public bool HasFrontMatter => Script?.FrontMatter != null;

        /// <summary>
        /// Gets or sets the output of the script.
        /// </summary>
        public string Content
        {
            get { return DynamicObject.GetSafeValue<string>(PageVariables.Content); }
            set { DynamicObject[PageVariables.Content] = value; }
        }

        /// <summary>
        /// Gets or sets the output extension. If null, by default the same as the input <see cref="Extension"/>.
        /// </summary>
        public string ContentExtension
        {
            get { return DynamicObject.GetSafeValue<string>(PageVariables.ContentExtension); }
            set { DynamicObject[PageVariables.ContentExtension] = value; }
        }

        public string Url
        {
            get { return DynamicObject.GetSafeValue<string>(PageVariables.Url); }
            set { DynamicObject[PageVariables.Url] = value; }
        }

        public bool UrlExplicit
        {
            get { return DynamicObject.GetSafeValue<bool>(PageVariables.UrlExplicit); }
            set { DynamicObject[PageVariables.UrlExplicit] = value; }
        }


        public string Layout
        {
            get { return DynamicObject.GetSafeValue<string>(PageVariables.Layout); }
            set { DynamicObject[PageVariables.Layout] = value; }
        }

        public string GetDestinationPath()
        {
            var url = Url;

            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Relative, out uri))
            {
                Site.Warning($"Invalid Url [{url}] for page [{Path}]. Reverting to page default.");
                url = Url = Path;
            }

            var isIndex = System.IO.Path.GetFileName(Url) == "index";
            if (!isIndex && 
                ContentExtension == Site.DefaultPageExtension && !UrlExplicit)
            {
                url += "/index";
            }
            url += ContentExtension;
            return url;
        }
    }
}