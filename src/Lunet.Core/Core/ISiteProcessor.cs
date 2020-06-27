namespace Lunet.Core
{
    /// <summary>
    /// Main interface for a pluggable processor on a <see cref="SiteObject"/>.
    /// </summary>
    public interface ISiteProcessor : ISitePluginCore
    {
        void Process(ProcessingStage stage);
    }
}