// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lunet.Core;
using Lunet.Helpers;
using Lunet.Watcher;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zio;

namespace Lunet.Server
{
    public static class SiteObjectExtensions
    {
        private const string LiveReload = "livereload";

        public static bool GetLiveReload(this SiteObject site)
        {
            return site.GetSafeValue<bool>(LiveReload);
        }

        public static void SetLiveReload(this SiteObject site, bool value)
        {
            site[LiveReload] = value;
        }
    }

    public class ServerPlugin : SitePlugin
    {
        private const string LiveReloadBasePath = "/__livereload__";
        private readonly CommandOption noWatchOption;
        private readonly HashSet<WebSocket> sockets;
        private readonly Lazy<WatcherPlugin> _watcher;

        public const string DefaultBaseUrl = "http://localhost:4000";

        public ServerPlugin(SiteObject site, Lazy<WatcherPlugin> watcher) : base(site)
        {
            _watcher = watcher;
            AppBuilders = new OrderedList<Action<IApplicationBuilder>>();

            sockets = new HashSet<WebSocket>();

            // Setup by default livereload
            Site.SetLiveReload(true);

            // Adds the server command
            ServerCommand = Site.CommandLine.Command("serve", newApp =>
            {
                newApp.Description = "Builds the website, runs a web server and watches for changes";
                newApp.HelpOption("-h|--help");
                newApp.Invoke = Server;
            }, false);

            noWatchOption = ServerCommand.Option("-n|--no-watch", "Disables watching files and triggering of a new run", CommandOptionType.NoValue);
        }

        public CommandLineApplication ServerCommand { get; }

        public int Server()
        {
            BuildSite(Site, true);
            try
            {
                var wwwDirectory = Site.OutputFileSystem.ConvertPathToInternal(UPath.Root);
                var hostBuilder = new WebHostBuilder()
                    .UseKestrel()
                    .UseWebRoot(wwwDirectory)
                    .UseContentRoot(wwwDirectory)
                    .UseUrls(Site.BaseUrl)
                    .Configure(Configure);

                // Setup the environment
                // TODO: access to Site.Scripts.SiteFunctions.LunetObject is too long!
                hostBuilder.UseEnvironment(Site.Scripts.SiteFunctions.LunetObject.Environment ?? "Development");

                // Enable server log only if log.server = true
                if (Site.Scripts.SiteFunctions.LogObject.GetSafeValue<bool>("server"))
                {
                    hostBuilder.ConfigureServices(collection => collection.Add(ServiceDescriptor.Singleton(Site.LoggerFactory)));
                }

                var host = hostBuilder.Build();

                host.Run();
            }
            catch (Exception ex)
            {
                Site.Error($"Error while starting server. Reason: {ex.GetReason()}");
                return 1;
            }

            return 0;
        }

        private void BuildSite(SiteObject site, bool server)
        {
            site.CommandLine.HandleCommonOptions();

            if (site.BaseUrl == null || !site.BaseUrlForce)
            {
                site.BaseUrl = DefaultBaseUrl;
            }
            if (site.BasePath == null || !site.BaseUrlForce)
            {
                site.BasePath = string.Empty;
            }

            if (!noWatchOption.HasValue())
            {
                if (site.GetLiveReload())
                {
                    SetupLiveReloadClient(site);
                    if (server)
                    {
                        SetupLiveReloadServer();
                    }
                }

                if (server)
                {
                    var watcher = _watcher.Value;
                    watcher.Start();
                    watcher.FileSystemEvents += (sender, args) =>
                    {
                        var newSite = site.Clone();
                        if (site.CanTrace())
                        {
                            site.Info($"Received file events [{args.FileEvents.Count}]");
                        }

                        try
                        {
                            BuildSite(newSite, false);

                            if (newSite.GetLiveReload())
                            {
                                OnSiteRebuildLiveReload(newSite);
                            }
                        }
                        catch (Exception ex)
                        {
                            site.Error($"Unexpected error while reloading the site. Reason: {ex.GetReason()}");
                        }
                    };
                }
            }

            site.Build();
        }

        public OrderedList<Action<IApplicationBuilder>> AppBuilders { get; }

        public void Configure(IApplicationBuilder builder)
        {
            // Allow to configure the pipeline
            foreach (var appBuilderAction in AppBuilders)
            {
                appBuilderAction(builder);
            }

            builder.UseStatusCodePagesWithReExecute(Site.ErrorRedirect);

            // By default we always serve files at last
            builder.UseFileServer(new FileServerOptions()
            {
                StaticFileOptions =
                {
                    ServeUnknownFileTypes = true,
                    DefaultContentType = "application/octet-stream",
                },
                FileProvider = new SiteFileProvider(Site)
            });
        }

        private void SetupLiveReloadClient(SiteObject site)
        {
            const string builtinsLiveReloadHtml = "builtins/livereload.sbn-html";
            site.Html.HeadIncludes.Add(builtinsLiveReloadHtml);

            var liveReloadUrl = new Uri(new Uri(site.BaseUrl.Replace("http:", "ws:")), LiveReloadBasePath).ToString();
            site.SetValue("livereload_url", liveReloadUrl, true);
        }

        private void SetupLiveReloadServer()
        {
            AppBuilders.Add(ConfigureLiveReload);
        }

        private async void OnSiteRebuildLiveReload(SiteObject site)
        { 
            var localSockets = new List<WebSocket>();

            lock (sockets)
            {
                localSockets.Clear();
                localSockets.AddRange(sockets);
            }

            foreach (var socket in localSockets)
            {
                if (socket.State == WebSocketState.Open)
                {
                    byte[] messageData = Encoding.UTF8.GetBytes("reload");
                    var outputBuffer = new ArraySegment<byte>(messageData);

                    try
                    {
                        await socket.SendAsync(outputBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch (ObjectDisposedException)
                    {

                    }
                }
            }
        }

        private void ConfigureLiveReload(IApplicationBuilder app)
        {
            app.UseWebSockets();
            app.Use(HandleWebSockets);
        }

        private async Task HandleWebSockets(HttpContext http, Func<Task> next)
        {
            if (http.WebSockets.IsWebSocketRequest && http.Request.Path == LiveReloadBasePath)
            {
                var webSocket = await http.WebSockets.AcceptWebSocketAsync();

                if (webSocket != null && webSocket.State == WebSocketState.Open)
                {
                    lock (sockets)
                    {
                        if (!sockets.Add(webSocket))
                        {
                            return;
                        }
                    }

                    await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("hello")), WebSocketMessageType.Text, true, CancellationToken.None);
                    while (webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.ReceiveAsync(new ArraySegment<byte>(new byte[1024]), CancellationToken.None);
                    }

                    if (webSocket.State == WebSocketState.CloseReceived)
                    {
                        try
                        {
                            await
                                webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
                        }
                        catch (ObjectDisposedException)
                        {

                        }
                    }

                    lock (sockets)
                    {
                        sockets.Remove(webSocket);
                    }
                }
            }
            else
            {
                // Nothing to do here, pass downstream.  
                await next();
            }
        }

    }
}