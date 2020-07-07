namespace Lunet.Core
{
    public class HelperObject : DynamicObject<SiteObject>
    {
        public HelperObject(SiteObject parent) : base(parent)
        {
            parent.SetValue(SiteVariables.Helpers, this, true);
            Head = parent.Scripts.CompileAnonymous("include 'builtins/head.sbn-html'");
        }

        public object Head
        {
            get => GetSafeValue<object>("Head");
            set => SetValue("Head", value);
        }
    }
}