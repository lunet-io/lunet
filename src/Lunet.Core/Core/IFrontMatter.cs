using Scriban;

namespace Lunet.Core
{
    public interface IFrontMatter
    {
        void Evaluate(TemplateContext context);
    }
}