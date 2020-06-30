using Lunet.Core;

namespace Lunet.Bundles
{
    public class BundleLink : DynamicObject<BundleObject>
    {
        public BundleLink(BundleObject parent, string type, string path, string url, string mode) : base(parent)
        {
            Type = type;
            Path = path;
            Url = url;
            Mode = mode ?? "";
        }

        public string Type
        {
            get => GetSafeValue<string>("type");
            set => this["type"] = value;
        }


        public string Path
        {
            get => GetSafeValue<string>("path");
            set => this["path"] = value;
        }

        public string Url
        {
            get => GetSafeValue<string>("url");
            set => this["url"] = value;
        }

        public string Mode
        {
            get => GetSafeValue<string>("mode") ?? "";
            set => this["mode"] = value;
        }

        public string Content { get; set; }

        public ContentObject ContentObject { get; set; }
    }
}