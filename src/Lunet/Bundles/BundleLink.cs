using Lunet.Core;

namespace Lunet.Bundles
{
    public class BundleLink : DynamicObject<BundleObject>
    {
        public BundleLink(BundleObject parent, string type, string url) : base(parent)
        {
            Type = type;
            Url = url;
        }

        public string Type
        {
            get { return GetSafeValue<string>("type"); }
            set { this["type"] = value; }
        }

        public string Url
        {
            get { return GetSafeValue<string>("url"); }
            set { this["url"] = value; }
        }
    }
}