using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Lunet.Helpers;
using Lunet.Scripts;
using Scriban.Functions;
using Scriban.Helpers;
using Scriban.Runtime;
using Scriban.Syntax;
using Zio;

namespace Lunet.Core
{
    [DebuggerDisplay("Content: {Path}")]
    public class ContentObject : DynamicObject
    {
        private ContentType contentType;

        private static readonly Regex ParsePostName = new Regex(@"^(\d{4})-(\d{2})-(\d{2})-(.+)\..+$");

        public ContentObject(SiteObject site, FileEntry sourceFileInfo, ScriptInstance scriptInstance = null)
        {
            Site = site ?? throw new ArgumentNullException(nameof(site));
            SourceFile = sourceFileInfo ?? throw new ArgumentNullException(nameof(sourceFileInfo));
            FrontMatter = scriptInstance?.FrontMatter;
            Script = scriptInstance?.Template;
            Dependencies = new List<ContentDependency>();
            ObjectType = ContentObjectType.File;

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

            Path = sourceFileInfo.Path;
            Length = SourceFile.Length;
            Extension = SourceFile.ExtensionWithDot?.ToLowerInvariant();
            ModifiedTime = SourceFile.LastWriteTime;
            ContentType = Site.ContentTypes.GetContentType(Extension);

            // Extract the section of this content
            Section = Path.GetFirstDirectory(out var pathInSection);
            if (pathInSection.IsEmpty)
            {
                Section = string.Empty;
                PathInSection = Path;
            }
            else
            {
                PathInSection = pathInSection;
            }
            Layout = Section;

            // Extract the default Url of this content
            // By default, for html content, we don't output a file but a directory
            var urlAsPath = (string)Path;

            var isHtml = Site.ContentTypes.IsHtmlContentType(ContentType);
            if (isHtml)
            {
                var name = Path.GetNameWithoutExtension();
                var isIndex = name == "index";
                if (isIndex)
                {
                    urlAsPath = Path.GetDirectory().FullName + "/";
                }
                else if (HasFrontMatter && !Site.UrlAsFile)
                {
                    if (!string.IsNullOrEmpty(Extension))
                    {
                        urlAsPath = urlAsPath.Substring(0, urlAsPath.Length - Extension.Length);
                    }
                    urlAsPath = PathUtil.NormalizeUrl(urlAsPath, true);
                }
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
        public ContentObject(SiteObject site, string section = null)
        {
            Site = site ?? throw new ArgumentNullException(nameof(site));
            Dependencies = new List<ContentDependency>();

            Section = section;

            ObjectType = ContentObjectType.Dynamic;

            // Replicate readonly values to the Scripting object
            InitializeReadOnlyVariables();
        }

        public SiteObject Site { get; }

        public FileEntry SourceFile { get; }

        public long Length { get; }

        public ContentObjectType ObjectType { get; }

        public IFrontMatter FrontMatter { get; set; }

        public UPath Path { get; }

        public DateTime ModifiedTime { get; }

        public string Extension { get; }

        public string Section { get; }

        public UPath PathInSection { get; }


        public bool Discard
        {
            get => GetSafeValue<bool>(FileVariables.Discard);
            set => this[FileVariables.Discard] = value;
        }

        /// <summary>
        /// Gets or sets the script attached to this page if any.
        /// </summary>
        public ScriptPage Script { get; }

        public ScriptObject ScriptObjectLocal { get; set; }

        public bool HasFrontMatter => FrontMatter != null;

        /// <summary>
        /// Gets or sets the output of the script.
        /// </summary>
        public string Content
        {
            get => GetSafeValue<string>(PageVariables.Content);
            set => this[PageVariables.Content] = value;
        }

        /// <summary>
        /// Gets or sets the summary of this page.
        /// </summary>
        public string Summary
        {
            get => GetSafeValue<string>(PageVariables.Summary);
            set => this[PageVariables.Summary] = value;
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
            get => GetSafeValue<string>(PageVariables.Url);
            set => this[PageVariables.Url] = value;
        }

        public DateTime Date
        {
            get => GetSafeValue<DateTime>(PageVariables.Date);
            set => this[PageVariables.Date] = value;
        }

        public int Weight
        {
            get => GetSafeValue<int>(PageVariables.Weight);
            set => this[PageVariables.Weight] = value;
        }

        public string Title
        {
            get => GetSafeValue<string>(PageVariables.Title);
            set => this[PageVariables.Title] = value;
        }

        public string Layout
        {
            get => GetSafeValue<string>(PageVariables.Layout);
            set => this[PageVariables.Layout] = value;
        }

        public string LayoutType
        {
            get => GetSafeValue<string>(PageVariables.LayoutType);
            set => this[PageVariables.LayoutType] = value;
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

        public UPath GetDestinationPath()
        {
            var urlAsPath = Url;

            Uri uri;
            if (!Uri.TryCreate(urlAsPath, UriKind.Relative, out uri))
            {
                Site.Warning($"Invalid Url [{urlAsPath}] for page [{Path}]. Reverting to page default.");
                urlAsPath = Url = Path.FullName;
            }

            if (HasFrontMatter && ContentType == ContentType.Html && !Site.UrlAsFile && urlAsPath.EndsWith("/"))
            {
                urlAsPath += "index" + Site.GetSafeDefaultPageExtension();
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