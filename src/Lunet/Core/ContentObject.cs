using System;
using System.Diagnostics;
using System.IO;
using Lunet.Helpers;
using Lunet.Plugins;
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
            SourceFileInfo = sourceFileInfo.Normalize();
            SourceFile = sourceFileInfo.FullName;
            ObjectType = ContentObjectType.File;
            Site = site;

            Path = RootDirectory.GetRelativePath(SourceFile, PathFlags.Normalize);
            Length = SourceFileInfo.Length;
            Extension = SourceFileInfo.Extension.ToLowerInvariant();
            ModifiedTime = SourceFileInfo.LastWriteTime;
            ContentType = Site.ContentTypes.GetContentType(Extension);

            // Extract the section of this content
            var sectionIndex = Path.IndexOf('/');
            if (sectionIndex >= 0)
            {
                Section = Path.Substring(0, sectionIndex);
                PathInSection = Path.Substring(sectionIndex + 1);
            }
            else
            {
                Section = string.Empty;
                PathInSection = Path;
            }
            Layout = Section;

            // Extract the default Url of this content
            // By default, for html content, we don't output a file but a directory
            Url = "/" + Path;

            // Replicate readonly values to the Scripting object
            InitializeVariables();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentObject"/> class that is not attached to a particular file.
        /// </summary>
        /// <param name="rootDirectoryInfo">The root directory information.</param>
        /// <param name="site">The site.</param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public ContentObject(DirectoryInfo rootDirectoryInfo, SiteObject site)
        {
            if (rootDirectoryInfo == null) throw new ArgumentNullException(nameof(rootDirectoryInfo));
            if (site == null) throw new ArgumentNullException(nameof(site));
            RootDirectory = rootDirectoryInfo;
            Site = site;

            ObjectType = ContentObjectType.Dynamic;

            // Replicate readonly values to the Scripting object
            InitializeVariables();
        }

        public FolderInfo RootDirectory { get; }

        public SiteObject Site { get; }

        public FileInfo SourceFileInfo { get; }

        public string SourceFile { get; }

        public long Length { get; }

        public ContentObjectType ObjectType { get; }

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
        public string ContentType
        {
            get { return DynamicObject.GetSafeValue<string>(PageVariables.ContentExtension); }
            set { DynamicObject[PageVariables.ContentExtension] = value; }
        }

        public string Url
        {
            get { return DynamicObject.GetSafeValue<string>(PageVariables.Url); }
            set { DynamicObject[PageVariables.Url] = value; }
        }

        public string Layout
        {
            get { return DynamicObject.GetSafeValue<string>(PageVariables.Layout); }
            set { DynamicObject[PageVariables.Layout] = value; }
        }

        public string GetDestinationPath()
        {
            var urlAsPath = Url;

            Uri uri;
            if (!Uri.TryCreate(urlAsPath, UriKind.Relative, out uri))
            {
                Site.Warning($"Invalid Url [{urlAsPath}] for page [{Path}]. Reverting to page default.");
                urlAsPath = Url = Path;
            }

            if (ContentType == ContentTypes.Html && !Site.UrlAsFile)
            {
                var isIndex = System.IO.Path.GetFileNameWithoutExtension(urlAsPath) == "index";
                if (!isIndex)
                {
                    var extension = System.IO.Path.GetExtension(urlAsPath);
                    if (extension != null)
                    {
                        urlAsPath = urlAsPath.Substring(0, urlAsPath.Length - extension.Length);
                    }
                    urlAsPath = PathUtil.NormalizeRelativePath(urlAsPath, true);
                    urlAsPath += "index" + Site.GetSafeDefaultPageExtension();
                }
            }

            // Make sure that destination path does not start by a /
            if (!uri.IsAbsoluteUri)
            {
                urlAsPath = PathUtil.NormalizeRelativePath(urlAsPath, false);
            }
            return urlAsPath;
        }

        private void InitializeVariables()
        {
            // Replicate readonly values to the Scripting object
            DynamicObject.SetValue(FileVariables.Length, Length, true);
            DynamicObject.SetValue(FileVariables.ModifiedTime, ModifiedTime, true);
            DynamicObject.SetValue(FileVariables.Path, Path, true);
            DynamicObject.SetValue(FileVariables.Extension, Extension, true);

            DynamicObject.SetValue(PageVariables.Section, Layout, true);
            DynamicObject.SetValue(PageVariables.PathInSection, PathInSection, true);
        }
    }
}