namespace Lunet.Runtime
{
    public class FileVariables
    {
        protected FileVariables() { }

        public const string Length = "length";

        public const string ModifiedTime = "modified_time";

        public const string Path = "path";

        public const string Extension = "ext";

        public const string Discard = "discard";
    }


    public class PageVariables : FileVariables
    {
        private PageVariables() {}

        public const string Page = "page";

        public const string Site = "site";

        public const string Url = "url";

        public const string UrlExplicit = "url_explicit";

        public const string Content = "content";

        public const string ContentExtension = "content_ext";

        public const string Layout = "layout";

        public const string LayoutType = "layout_type";

        public const string DefaultLayout = "default_layout";

        public const string PathLayout = "path_layout";
    }
}