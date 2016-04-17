using System;
using System.Diagnostics;
using System.IO;
using Lunet.Helpers;
using Textamina.Scriban.Helpers;
using Textamina.Scriban.Runtime;

namespace Lunet.Runtime
{
    [DebuggerDisplay("Content: {Path}")]
    public class ContentObject : LunetObject
    {
        public ContentObject(DirectoryInfo rootDirectoryInfo, FileInfo sourceFileInfo, SiteObject site)
        {
            if (rootDirectoryInfo == null) throw new ArgumentNullException(nameof(rootDirectoryInfo));
            if (sourceFileInfo == null) throw new ArgumentNullException(nameof(sourceFileInfo));
            if (site == null) throw new ArgumentNullException(nameof(site));
            RootDirectoryInfo = rootDirectoryInfo;
            RootDirectory = RootDirectoryInfo.FullName;
            SourceFileInfo = sourceFileInfo;
            SourceFile = sourceFileInfo.FullName;
            Site = site;

            Path = PathUtil.GetRelativePath(RootDirectory, SourceFile, true);
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
            SetValue(FileVariables.Length, Length, true);
            SetValue(FileVariables.ModifiedTime, ModifiedTime, true);
            SetValue(FileVariables.Path, Path, true);
            SetValue(FileVariables.Extension, Extension, true);

            SetValue(PageVariables.Section, Layout, true);
            SetValue(PageVariables.PathInSection, PathInSection, true);
        }

        public DirectoryInfo RootDirectoryInfo { get; }

        public string RootDirectory { get; }

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
            get { return GetSafe<bool>(FileVariables.Discard); }
            set { this[FileVariables.Discard] = value; }
        }
       

        /// <summary>
        /// Gets or sets the script attached to this page if any.
        /// </summary>
        public ScriptPage Script { get; set; }


        public ScriptObject ScriptObject { get; set; }

        public bool HasFrontMatter => Script?.FrontMatter != null;

        /// <summary>
        /// Gets or sets the output of the script.
        /// </summary>
        public string Content
        {
            get { return GetSafe<string>(PageVariables.Content); }
            set { this[PageVariables.Content] = value; }
        }

        /// <summary>
        /// Gets or sets the output extension. If null, by default the same as the input <see cref="Extension"/>.
        /// </summary>
        public string ContentExtension
        {
            get { return GetSafe<string>(PageVariables.ContentExtension); }
            set { this[PageVariables.ContentExtension] = value; }
        }

        public string Url
        {
            get { return GetSafe<string>(PageVariables.Url); }
            set { this[PageVariables.Url] = value; }
        }

        public bool UrlExplicit
        {
            get { return GetSafe<bool>(PageVariables.UrlExplicit); }
            set { this[PageVariables.UrlExplicit] = value; }
        }


        public string Layout
        {
            get { return GetSafe<string>(PageVariables.Layout); }
            set { this[PageVariables.Layout] = value; }
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