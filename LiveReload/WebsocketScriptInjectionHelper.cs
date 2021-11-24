using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LiveReload.Models;
using Microsoft.AspNetCore.Http;

namespace LiveReload
{
    public static class WebsocketScriptInjectionHelper
    {
        private const string StrJbartelsMarker = "<!-- Jbartels Live Reload -->";
        private const string StrBodyMarker = "</body>";

        private static readonly byte[] BodyBytes = Encoding.UTF8.GetBytes(StrBodyMarker);
        private static readonly byte[] MarkerBytes = Encoding.UTF8.GetBytes(StrJbartelsMarker);
        private static string _clientScriptString = string.Empty;
        
        /// <summary>
        ///     Injects WebSocket Refresh code into JavaScript document
        /// </summary>
        public static string InjectLiveReloadScript(string html, HttpContext context)
        {
            if (html.Contains(StrJbartelsMarker))
                return html;

            string script = GetWebSocketClientJavaScript(context);
            html = html.Replace(StrBodyMarker, script);

            return html;
        }

        /// <summary>
        ///     Adds Live Reload WebSocket script into the page before the body tag.
        /// </summary>
        public static Task InjectLiveReloadScriptAsync(ReadOnlyMemory<byte> buffer, HttpContext context, Stream baseStream)
        {
            return InjectLiveReloadScriptAsync(buffer.ToArray(), context, baseStream);
        }

        /// <summary>
        ///     Adds Live Reload WebSocket script into the page before the body tag.
        /// </summary>
        /// <returns></returns>
        private static async Task InjectLiveReloadScriptAsync(byte[] buffer, HttpContext context, Stream baseStream)
        {
            var index = buffer.LastIndexOf(MarkerBytes);

            if (index > -1)
            {
                await baseStream.WriteAsync(buffer);
                return;
            }

            index = buffer.LastIndexOf(BodyBytes);
            
            if (index == -1)
            {
                await baseStream.WriteAsync(buffer);
                return;
            }

            var endIndex = index + BodyBytes.Length;

            // Write pre-marker buffer
            await baseStream.WriteAsync(buffer.AsMemory(0, index - 1));
            
            // Write the injected script
            var scriptBytes = Encoding.UTF8.GetBytes(GetWebSocketClientJavaScript(context));
            await baseStream.WriteAsync(scriptBytes);

            // Write the rest of the buffer/HTML doc
            await baseStream.WriteAsync(buffer.AsMemory(endIndex, buffer.Length - endIndex));
        }

        private static int LastIndexOf<T>(this T[] array, T[] sought) where T : IEquatable<T> => array.AsSpan().LastIndexOf(sought);

        // ReSharper disable once CognitiveComplexity
        public static string GetWebSocketClientJavaScript(HttpContext context, bool returnScriptOnly = false)
        {
            var config = LiveReloadConfiguration.Current;

            var host = context.Request.Host;
            string hostString;
            
            if (!string.IsNullOrEmpty(config.WebSocketHost))
            {
                hostString = config.WebSocketHost + config.WebSocketUrl;
            }
            else
            {
                var prefix = context.Request.IsHttps ? "wss" : "ws";
                hostString = $"{prefix}://{host.Host}:{host.Port}" + config.WebSocketUrl;
            }

            if (string.IsNullOrEmpty(_clientScriptString))
            {
                lock (_clientScriptString)//Load `/LiveReloadClientScript.js` from resource stream
                {
                    if (string.IsNullOrEmpty(_clientScriptString))
                    {
                        using var scriptStream = Assembly.GetExecutingAssembly()
                            .GetManifestResourceStream("LiveReload.LiveReloadClientScript.js");
                        
                        if (scriptStream == null) //something went wrong..
                            throw new InvalidDataException("Unable to load LiveReloadClientScript.js Resource");

                        var buffer = new byte[scriptStream.Length];
                        scriptStream.Read(buffer, 0, buffer.Length);
                        _clientScriptString = Encoding.UTF8.GetString(buffer);
                    }
                }
            }

            if (returnScriptOnly)
                return _clientScriptString.Replace("{0}", hostString);

            // otherwise return the embeddable script block string that replaces the ending </body> tag
            var script = @"
<!-- jbartels Live Reload -->
";

            if (string.IsNullOrEmpty(config.LiveReloadScriptUrl))
                script += "<script>\n" + _clientScriptString.Replace("{0}", hostString) + "\n</script>";
            else
                script += $"<script src=\"{config.LiveReloadScriptUrl}\"></script>";

            script += @"
<!-- End Live Reload -->

</body>";

            return script;
        }
    }
}