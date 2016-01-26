namespace Lunet.Plugins
{
    public enum PageProcessResult
    {
        /// <summary>
        /// The page was not processed by the <see cref="IPageProcessor.TryProcess"/>
        /// </summary>
        None,

        /// <summary>
        /// The page was processed by the <see cref="IPageProcessor.TryProcess"/> 
        /// and allow other processors to transform it.
        /// </summary>
        Continue,

        /// <summary>
        /// The page was processed by the <see cref="IPageProcessor.TryProcess"/> 
        /// but we break the any further processing of this page
        /// </summary>
        Break,
    }
}