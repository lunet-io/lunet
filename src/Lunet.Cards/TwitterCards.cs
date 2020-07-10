using Lunet.Core;

namespace Lunet.Cards
{
    public class TwitterCards : DynamicObject<CardsPlugin>
    {
        public TwitterCards(CardsPlugin parent) : base(parent)
        {
            parent.SetValue("twitter", this, true);
        }

        public bool Enable
        {
            get => GetSafeValue<bool>("enable");
            set => SetValue("enable", value);
        }

        // <meta name="twitter:card" content="summary_large_image">
        public string Card
        {
            get => GetSafeValue<string>("card");
            set => SetValue("card", value);
        }

        // <meta name="twitter:site" content="@xoofx">
        public string User
        {
            get => GetSafeValue<string>("user");
            set => SetValue("user", value);
        }

        // <meta name="twitter:title" content="kalk - calculator">
        public string Title
        {
            get => GetSafeValue<string>("title");
            set => SetValue("title", value);
        }

        // <meta name="twitter:description" content="kalk is a powerfull command-line calculator application for developers.">
        public string Description
        {
            get => GetSafeValue<string>("description");
            set => SetValue("description", value);
        }

        // <meta name="twitter:image" content="{{ '/img/twitter-banner.jpg' | string.prepend site.baseurl | string.prepend site.url }}">
        public string Image
        {
            get => GetSafeValue<string>("image");
            set => SetValue("image", value);
        }

        // <meta name="twitter:image:alt" content="kalk - calculator">
        public string ImageAlt
        {
            get => GetSafeValue<string>("image_alt");
            set => SetValue("image_alt", value);
        }
    }
}