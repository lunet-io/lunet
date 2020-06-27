namespace Lunet.Core
{
    /// <summary>
    /// Processing stages
    /// </summary>
    public enum ProcessingStage
    {
        /// <summary>
        /// This stage is happening right before the site is initialized from the configuration file.
        /// </summary>
        BeforeInitializing,

        /// <summary>
        /// This stage is happening once the site has been initialized from the configuration but no content has been loaded yet.
        /// </summary>
        BeforeLoadingContent,

        /// <summary>
        /// This stage is happening after all content has been loaded. But the content has not been yet processed.
        /// Then, the registered content processors <see cref="IContentProcessor"/> are called after this stage.
        /// </summary>
        BeforeProcessingContent,
        
        /// <summary>
        /// This stage is called after all content has been processed by <see cref="IContentProcessor"/>.
        /// </summary>
        AfterProcessingContent,
    }
}