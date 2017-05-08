using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Lunet.Helpers;
using Scriban.Functions;
using Scriban.Helpers;
using Scriban.Model;
using Scriban.Runtime;

namespace Lunet.Core
{
    [DebuggerDisplay("Content: {Path}")]
    public class ContentObject : DynamicObject
    {
        private ContentType contentType;

        private static readonly Regex ParsePostName = new Regex(@"^(\d{4})-(\d{2})-(\d{2})-(.+)\..+$");

        public ContentObject(SiteObject site, DirectoryInfo rootDirectoryInfo, FileInfo sourceFileInfo)
        {
            if (rootDirectoryInfo == null) throw new ArgumentNullException(nameof(rootDirectoryInfo));
            if (sourceFileInfo == null) throw new ArgumentNullException(nameof(sourceFileInfo));
            if (site == null) throw new ArgumentNullException(nameof(site));
            RootDirectory = rootDirectoryInfo;
            SourceFileInfo = sourceFileInfo.Normalize();
            SourceFile = sourceFileInfo.FullName;
            Dependencies = new List<ContentDependency>();
            ObjectType = ContentObjectType.File;
            Site = site;

            // TODO: Make this part pluggable
            // Parse a standard blog text
            var match = ParsePostName.Match(sourceFileInfo.Name);
            if (match.Success)
            {
                var year = int.Parse(match.Groups[1].Value);
                var month = int.Parse(match.Groups[2].Value);
                var day = int.Parse(match.Groups[3].Value);
                var title = match.Groups[4].Value;
                Date = new DateTime(year, month, day);
                Title = StringFunctions.Capitalize(title.Replace('-',' '));
            }
            else
            {
                Date = DateTime.Now;
            }

            Path = RootDirectory.GetRelativePath(SourceFile, PathFlags.Normalize);
            Length = SourceFileInfo.Length;
            Extension = SourceFileInfo.Extension.ToLowerInvariant();
            ModifiedTime = SourceFileInfo.LastWriteTime;
            ContentType = Site.ContentTypes.GetContentType(Extension);

            var isHtml = Site.ContentTypes.IsHtmlContentType(ContentType);

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
            var urlAsPath = "/" + Path;

            if (isHtml && !Site.UrlAsFile)
            {
                var isIndex = System.IO.Path.GetFileNameWithoutExtension(urlAsPath) == "index";
                if (isIndex)
                {
                    urlAsPath = System.IO.Path.GetDirectoryName(urlAsPath);
                }
                else if (!string.IsNullOrEmpty(Extension))
                {
                    urlAsPath = urlAsPath.Substring(0, urlAsPath.Length - Extension.Length);
                }
                urlAsPath = PathUtil.NormalizeUrl(urlAsPath, true);
            }
            Url = urlAsPath;

            // Replicate readonly values to the Scripting object
            InitializeReadOnlyVariables();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentObject"/> class that is not attached to a particular file.
        /// </summary>
        /// <param name="site">The site.</param>
        /// <param name="rootDirectoryInfo">The root directory information.</param>
        /// <param name="section"></param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public ContentObject(SiteObject site, DirectoryInfo rootDirectoryInfo, string section = null)
        {
            if (rootDirectoryInfo == null) throw new ArgumentNullException(nameof(rootDirectoryInfo));
            if (site == null) throw new ArgumentNullException(nameof(site));
            RootDirectory = rootDirectoryInfo;
            Dependencies = new List<ContentDependency>();
            Site = site;

            Section = section;

            ObjectType = ContentObjectType.Dynamic;

            // Replicate readonly values to the Scripting object
            InitializeReadOnlyVariables();
        }

        public FolderInfo RootDirectory { get; }

        public SiteObject Site { get; }

        public FileInfo SourceFileInfo { get; }

        public string SourceFile { get; }

        public long Length { get; }

        public ContentObjectType ObjectType { get; }

        public string Path { get; }

        public DateTime ModifiedTime { get; }

        public string Extension { get; }

        public string Section { get; }

        public string PathInSection { get; }


        public bool Discard
        {
            get { return GetSafeValue<bool>(FileVariables.Discard); }
            set { this[FileVariables.Discard] = value; }
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
            get { return GetSafeValue<string>(PageVariables.Content); }
            set { this[PageVariables.Content] = value; }
        }

        /// <summary>
        /// Gets or sets the summary of this page.
        /// </summary>
        public string Summary
        {
            get { return GetSafeValue<string>(PageVariables.Summary); }
            set { this[PageVariables.Summary] = value; }
        }

        /// <summary>
        /// Gets or sets the output extension. If null, by default the same as the input <see cref="Extension"/>.
        /// </summary>
        public ContentType ContentType
        {
            get { return contentType; }

            set
            {
                contentType = value;
                // Special case, ContentType is seen as readonly in scripts
                SetValue(PageVariables.ContentType, contentType.Name, true);
            }
        }

        public string Url
        {
            get { return GetSafeValue<string>(PageVariables.Url); }
            set { this[PageVariables.Url] = value; }
        }

        public DateTime Date
        {
            get { return GetSafeValue<DateTime>(PageVariables.Date); }
            set { this[PageVariables.Date] = value; }
        }

        public int Weight
        {
            get { return GetSafeValue<int>(PageVariables.Weight); }
            set { this[PageVariables.Weight] = value; }
        }

        public string Title
        {
            get { return GetSafeValue<string>(PageVariables.Title); }
            set { this[PageVariables.Title] = value; }
        }

        public string Layout
        {
            get { return GetSafeValue<string>(PageVariables.Layout); }
            set { this[PageVariables.Layout] = value; }
        }

        public string LayoutType
        {
            get { return GetSafeValue<string>(PageVariables.LayoutType); }
            set { this[PageVariables.LayoutType] = value; }
        }

        public List<ContentDependency> Dependencies { get; }

        public void ChangeContentType(ContentType newContentType)
        {
            ContentType = newContentType;
            var newExtension = ContentType.Name;
            if (ContentType == ContentType.Html)
            {
                newExtension = Site.GetSafeDefaultPageExtension();
            }
            if (!Url.EndsWith("/"))
            {
                Url = System.IO.Path.ChangeExtension(Url, newExtension);
            }
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

            if (ContentType == ContentType.Html && !Site.UrlAsFile)
            {
                if (!urlAsPath.EndsWith("/"))
                {
                    var extension = System.IO.Path.GetExtension(urlAsPath);
                    if (!string.IsNullOrEmpty(extension))
                    {
                        urlAsPath = urlAsPath.Substring(0, urlAsPath.Length - extension.Length);
                    }
                }
                urlAsPath = PathUtil.NormalizeRelativePath(urlAsPath, true);
                urlAsPath += "index" + Site.GetSafeDefaultPageExtension();
            }

            // Make sure that destination path does not start by a /
            if (!uri.IsAbsoluteUri)
            {
                urlAsPath = PathUtil.NormalizeRelativePath(urlAsPath, false);
            }
            return urlAsPath;
        }

        private void InitializeReadOnlyVariables()
        {
            // Replicate readonly values to the Scripting object
            SetValue(FileVariables.Length, Length, true);
            SetValue(FileVariables.ModifiedTime, ModifiedTime, true);
            SetValue(FileVariables.Path, Path, true);
            SetValue(FileVariables.Extension, Extension, true);

            SetValue(PageVariables.Section, Layout, true);
            SetValue(PageVariables.PathInSection, PathInSection, true);
        }
    }
}