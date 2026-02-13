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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lunet.Server;

public class SiteServerService : ISiteService
{
    private readonly SiteConfiguration _configuration;
    private const string LiveReloadBasePath = "/__livereload__";
    private readonly HashSet<WebSocket> _sockets;
    private IWebHost _host;
    private CancellationTokenSource _tokenSource;
        
    public const string DefaultBaseUrl = "http://localhost:4000";
    public const string DefaultRedirect = "/404.html";
    public const string DefaultEnvironment = "dev";
        
    public SiteServerService(SiteConfiguration configuration)
    {
        _configuration = configuration;
        AppBuilders = new OrderedList<Action<IApplicationBuilder>>();
        _sockets = new HashSet<WebSocket>();
        BaseUrl = DefaultBaseUrl;
        Environment = DefaultEnvironment;
        Logging = false;
        BasePath = "";
        LiveReload = true;
        _tokenSource = new CancellationTokenSource();
    }


    public bool Update(SiteObject from)
    {
        var newErrorRedirect = from.ErrorRedirect ?? DefaultRedirect;
        var newBaseUrl = from.BaseUrl ?? DefaultBaseUrl;
        var newEnvironement = from.Environment ?? DefaultEnvironment;
        var newLogging = from.Builtins.LogObject.GetSafeValue<bool>("server");
        var newLiveReload = from.GetLiveReload();

        var needNewHost = _host == null ||
                          ErrorRedirect != newErrorRedirect ||
                          BaseUrl != newBaseUrl ||
                          Environment != newEnvironement ||
                          Logging != newLogging ||
                          LiveReload != newLiveReload;

        ErrorRedirect = newErrorRedirect;
        BaseUrl = newBaseUrl;
        BasePath = from.BasePath ?? "";
        Environment = newEnvironement;
        Logging = newLogging;
        LiveReload = newLiveReload;

        return needNewHost;
    }
        
    public void StartOrUpdate(CancellationToken token)
    {
        AppBuilders.Clear();

        if (LiveReload)
        {
            AppBuilders.Add(ConfigureLiveReload);
        }

        if (_host != null)
        {
            ShutdownAndWaitForShutdown();
            // Recreate a token source combined with the global token source
            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        }

        _host = CreateWebHost();
        RunAsync(_host, _tokenSource.Token, "Lunet server started.");
    }

    public void ShutdownAndWaitForShutdown()
    {
        _tokenSource.Cancel();
        _host.WaitForShutdown();
        _host.Dispose();

        lock (_sockets)
        {
            foreach (var socket in _sockets)
            {
                socket.Dispose();
            }

            _sockets.Clear();
        }

        _host = null;
    }

    public string ErrorRedirect { get; set; }

    public string BaseUrl { get; set; }

    public string BasePath { get; set; }
        
    public string Environment { get; set; }

    public bool Logging { get; set; }

    public bool LiveReload { get; set; }

    private OrderedList<Action<IApplicationBuilder>> AppBuilders { get; }

    private async void RunAsync(IWebHost host, CancellationToken token, string? startupMessage)
    {
        var logger = _configuration;
        try
        {
            await host.StartAsync(token);

            ICollection<string> addresses = host.ServerFeatures.Get<IServerAddressesFeature>()?.Addresses;
            if (addresses != null)
            {
                foreach (string str in (IEnumerable<string>)addresses)
                    logger.Info($"Now listening on: {str}{BasePath}");
            }
            if (!string.IsNullOrEmpty(startupMessage))
                logger.Info(startupMessage);

            await WaitForTokenShutdownAsync(host, token);
            logger.Info("Lunet server stopped.");
        }
        finally
        {
            if (host is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            else
                host.Dispose();
        }
    }

    private static async Task WaitForTokenShutdownAsync(IWebHost host, CancellationToken token)
    {
        IHostApplicationLifetime requiredService = host.Services.GetRequiredService<IHostApplicationLifetime>();
        token.Register((Action<object>)(state =>
        {
            ((IHostApplicationLifetime) state).StopApplication();
        }), (object)requiredService);
        TaskCompletionSource completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        requiredService.ApplicationStopping.Register((Action<object>)(obj => ((TaskCompletionSource)obj).TrySetResult()), (object)completionSource);
        await completionSource.Task;
        await host.StopAsync();
    }

    public void Dispose()
    {
    }


    private IWebHost CreateWebHost()
    {
        var hostBuilder = new WebHostBuilder()
            .UseKestrel()
            .UseUrls(BaseUrl ?? DefaultBaseUrl)
            .Configure(ConfigureWebHost);

        // Setup the environment
        // TODO: access to Site.Scripts.SiteFunctions.LunetObject is too long!
        hostBuilder.UseEnvironment(Environment ?? DefaultEnvironment);

        // Active compression
        hostBuilder.ConfigureServices(services =>
        {
            services.AddResponseCompression();
        });

        return hostBuilder.Build();
    }
        
    private void ConfigureWebHost(IApplicationBuilder app)
    {
        // Allow to configure the pipeline
        foreach (var appBuilderAction in AppBuilders)
        {
            appBuilderAction(app);
        }

        app.UseStatusCodePagesWithReExecute(ErrorRedirect);

        app.UseResponseCompression();

        // By default we always serve files at last
        app.UseFileServer(new FileServerOptions()
        {
            RequestPath = BasePath,
            StaticFileOptions =
            {
                ServeUnknownFileTypes = true,
                DefaultContentType = "application/octet-stream",
            },
            FileProvider = new SiteFileProvider(_configuration.FileSystems.OutputFileSystem)
        });
    }
        
    public static void SetupLiveReloadClient(SiteObject site)
    {
        const string builtinsLiveReloadHtml = "_builtins/livereload.sbn-html";
        site.Html.Head.Includes.Add(builtinsLiveReloadHtml);

        var liveReloadUrl = new Uri(new Uri(site.BaseUrl.Replace("http:", "ws:")), LiveReloadBasePath).ToString();
        site.SetValue("livereload_url", liveReloadUrl, true);
    }

    public async void NotifyReloadToClients()
    {
        var localSockets = new List<WebSocket>();

        lock (_sockets)
        {
            localSockets.Clear();
            localSockets.AddRange(_sockets);
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
                lock (_sockets)
                {
                    if (!_sockets.Add(webSocket))
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

                lock (_sockets)
                {
                    _sockets.Remove(webSocket);
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
