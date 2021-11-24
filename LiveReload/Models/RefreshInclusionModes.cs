namespace LiveReload.Models
{
    public enum RefreshInclusionModes
    {
        // (default) changed file continues down the list of configuration rules to determine on whether the browser refreshes
        ContinueProcessing,

        // Explicitly lets you specify to **not** refresh the browser on this changed file.
        DontRefresh
    }
}