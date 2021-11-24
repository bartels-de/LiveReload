using System;
using System.Threading;

namespace LiveReload.Models
{
    public sealed class LiveReloadConfiguration
    {
        /// <summary>
        ///     Current configuration instance accessible through out the middleware
        /// </summary>
        public static LiveReloadConfiguration Current { get; set; }


        /// <summary>
        ///     Determines whether live reload is enabled. If off, there's no
        ///     overhead in this middleware and it simply passes through requests.
        /// </summary>
        public bool LiveReloadEnabled { get; set; } = true;

        /// <summary>
        ///     Optional - the folder to monitor for file changes. By default
        ///     this value is set to the Web application root folder (ContentRootPath)
        /// </summary>
        public string FolderToMonitor { get; set; }


        /// <summary>
        ///     Comma delimited list of file extensions that the file watcher
        ///     responds to.
        ///     Note the `.live` extension which is used for server restarts. Please
        ///     make sure you always add that to your list or server reloads won't work.
        /// </summary>
        public string ClientFileExtensions { get; set; } = ".cshtml,.css,.js,.htm,.html,.ts,.razor";

        /// <summary>
        ///     Optional filter that allows you to examine each file that has been changed
        ///     and decide whether you want to refresh the browser. Options to not refresh,
        ///     force refresh or continue with config rules.
        ///     The path passed in as a string is an OS Path.
        /// </summary>
        public Func<string, FileInclusionModes> FileInclusionFilter { get; set; } = null;


        /// <summary>
        ///     Optional filter that lets you examine each HTML request and decide whether you
        ///     want to allow browser refresh to occur or not. If not, the WebSocket script
        ///     to refresh the browser is not injected into the HTML page.
        ///     The path passed in as a string is a Root Relative Web Path.
        /// </summary>
        public Func<string, RefreshInclusionModes> RefreshInclusionFilter { get; set; } = null;


        /// <summary>
        ///     The timeout to wait before refreshing the page in the browser
        ///     for Razor page re-compilation requests that don't restart the server
        ///     This shouldn't be necessary as Razor server recompilation should block
        ///     the page from being served while it's being recompiled. You'll see the
        ///     browser 'waiting to connect' until the page is ready to load
        ///     Only bump this value if you have problems with Razor page refreshes
        ///     and it's suggested you bump this value up slowly to find your sweet
        ///     spot - it should only have to be long enough for ASP.NET to get to the
        ///     file to recompile before the page refreshes.
        /// </summary>
        public int ServerRefreshTimeout { get; set; } = 0;

        /// <summary>
        ///     Url that loads the live reload script.
        ///     Leave blank to serve inline
        /// </summary>
        public string LiveReloadScriptUrl { get; set; } = "/__livereloadscript";

        /// <summary>
        ///     The URL used for the Web Socket connection on the page to refresh
        /// </summary>
        public string WebSocketUrl { get; set; } = "/__livereload";

        /// <summary>
        ///     Optional WebSocket host. Use this if you are on an Https2 connection
        ///     to point at a http connection. "ws://localhost:5000"
        /// </summary>
        public string WebSocketHost { get; set; }

        /// <summary>
        ///     Optionally passed in token to cancel out of pending Web Socket
        ///     wait operations in order to allow for a controlled shutdown.
        ///     Provide a cancellation token from Shutdown services or a hosted
        ///     service.
        /// </summary>
        public CancellationToken ShutdownCancellationToken { get; set; }
    }
}