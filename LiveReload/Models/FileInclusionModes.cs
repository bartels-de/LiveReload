namespace LiveReload.Models
{
    /// <summary>
    ///     Modes that determine how the FileIncludeFilter should behave for a given changed file.
    ///     This filter allows to explicitly force the browser to be refreshed for a specific changed
    ///     file or location, or to explicitly reject or exclude a file from refreshing.
    ///     This can be useful to handle non-standard files that are hard to code into an extension
    ///     rule, or to explicitly exclude file or groups of files from auto-refreshing (for example
    ///     keeping a certain folder from not being checked).
    /// </summary>
    public enum FileInclusionModes
    {
        // (default) changed file continues down the list of configuration rules to determine on whether the browser refreshes
        ContinueProcessing,

        // Explicitly lets you specify that the browser should be refreshed when this file has changed.
        ForceRefresh,

        // Explicitly lets you specify to **not** refresh the browser on this changed file.
        DontRefresh
    }
}