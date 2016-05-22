using Lunet.Core;

namespace Lunet.Bundles
{
    public class BundleLink : DynamicObject<BundleObject>
    {
        public BundleLink(BundleObject parent, string type, string path, string url) : base(parent)
        {
            Type = type;
            Path = path;
            Url = url;
        }

        public string Type
        {
            get { return GetSafeValue<string>("type"); }
            set { this["type"] = value; }
        }


        public string Path
        {
            get { return GetSafeValue<string>("path"); }
            set { this["path"] = value; }
        }

        public string Url
        {
            get { return GetSafeValue<string>("url"); }
            set { this["url"] = value; }
        }

        public string Content { get; set; }

        public ContentObject ContentObject { get; set; }
    }
}