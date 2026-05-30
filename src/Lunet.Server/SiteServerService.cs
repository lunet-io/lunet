// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lunet.Core;
using Lunet.Helpers;
using PicoServer;

namespace Lunet.Server;

public class SiteServerService : ISiteService
{
    private readonly SiteConfiguration _configuration;
    private const string LiveReloadBasePath = "/__livereload__"; //any path
    private readonly HashSet<WebSocket> _sockets;
    private WebAPIServer? _server;
    private Task? _serverTask;
    private CancellationTokenSource _tokenSource;

    public const string DefaultBaseUrl = "http://localhost:4000";
    public const string DefaultRedirect = "/404.html";
    public const string DefaultEnvironment = "dev";

    public SiteServerService(SiteConfiguration configuration)
    {
        _configuration = configuration;
        _sockets = new HashSet<WebSocket>();
        BaseUrl = DefaultBaseUrl;
        Environment = DefaultEnvironment;
        Logging = false;
        BasePath = "";
        LiveReload = true;
        ErrorRedirect = DefaultRedirect;
        _tokenSource = new CancellationTokenSource();
    }

    public bool Update(SiteObject from)
    {
        var newErrorRedirect = from.ErrorRedirect ?? DefaultRedirect;
        var newBaseUrl = from.BaseUrl ?? DefaultBaseUrl;
        var newEnvironment = from.Environment ?? DefaultEnvironment;
        var newLogging = from.Builtins.LogObject.GetSafeValue<bool>("server");
        var newLiveReload = from.GetLiveReload();

        var needNewHost = _server == null ||
                          ErrorRedirect != newErrorRedirect ||
                          BaseUrl != newBaseUrl ||
                          Environment != newEnvironment ||
                          Logging != newLogging ||
                          LiveReload != newLiveReload;

        ErrorRedirect = newErrorRedirect;
        BaseUrl = newBaseUrl;
        BasePath = from.BasePath ?? "";
        Environment = newEnvironment;
        Logging = newLogging;
        LiveReload = newLiveReload;

        return needNewHost;
    }

    public Task StartOrUpdateAsync(CancellationToken token)
    {
        if (_server != null)
        {
            ShutdownAndWaitForShutdown();
        }

        _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

        _server = CreateWebServer();
        _serverTask = Task.Run(() => _server.StartServer(GetPortFromUrl(BaseUrl)), _tokenSource.Token);

        var logger = _configuration;
        logger.Info($"Now listening on: {BaseUrl}{BasePath}");
        logger.Info("Lunet server started.");

        return Task.CompletedTask;
    }

    public void ShutdownAndWaitForShutdown()
    {
        _tokenSource.Cancel();

        try
        {
            _server?.StopServer();
            _serverTask?.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            // Expected when shutting down via cancellation
        }

        lock (_sockets)
        {
            foreach (var socket in _sockets)
            {
                socket.Dispose();
            }
            _sockets.Clear();
        }

        _server = null;
        _serverTask = null;
    }

    public string ErrorRedirect { get; set; }
    public string BaseUrl { get; set; }
    public string BasePath { get; set; }
    public string Environment { get; set; }
    public bool Logging { get; set; }
    public bool LiveReload { get; set; }

    private int GetPortFromUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.Port > 0 ? uri.Port : 8090;
        }
        return 8090;
    }

    private string? GetPhysicalOutputPath()
    {
        var fs = _configuration.FileSystems.OutputFileSystem;
        if (fs == null) return null;

        try
        {
            var physicalPath = fs.ConvertPathToInternal("/");
            if (!string.IsNullOrEmpty(physicalPath) && Directory.Exists(physicalPath))
                return physicalPath;
        }
        catch { }

        var defaultPath = Path.Combine(Directory.GetCurrentDirectory(), "_lunet", "build", "www");
        if (Directory.Exists(defaultPath))
            return defaultPath;

        return null;
    }

    private WebAPIServer CreateWebServer()
    {
        var server = new WebAPIServer();

        if (LiveReload)
        {
            server.enableWebSocket = true;
            server.WsOnConnectionChanged += OnConnectionChanged;
            server.WsOnMessage += OnMessageReceived;
        }

        var outputPath = GetPhysicalOutputPath();
        if (!string.IsNullOrEmpty(outputPath) && Directory.Exists(outputPath))
        {
            server.AddStaticFiles(BasePath, outputPath);
        }

        return server;
    }

    private async Task OnConnectionChanged(string clientId, bool connected)
    {
        if (connected)
        {
            if (_server != null)
            {
                await _server.WsSendToClientAsync(clientId, "hello");
            }
        }
    }

    private async Task OnMessageReceived(string clientId, string message, Func<string, Task> reply)
    {
        if (message == "hello")
        {
            await reply("hello");
        }
    }

    public static void SetupLiveReloadClient(SiteObject site)
    {
        const string builtinsLiveReloadHtml = "_builtins/livereload.sbn-html";
        site.Html.Head.Includes.Add(builtinsLiveReloadHtml);

        var baseUrl = site.BaseUrl ?? DefaultBaseUrl;
        var liveReloadUrl = new Uri(new Uri(baseUrl.Replace("http:", "ws:", StringComparison.OrdinalIgnoreCase)), LiveReloadBasePath).ToString();
        site.SetValue("livereload_url", liveReloadUrl, true);
    }

    public async Task NotifyReloadToClients()
    {
        if (_server == null || !LiveReload) return;

        await _server.WsBroadcastAsync("reload");
    }

    public void Dispose()
    {
        ShutdownAndWaitForShutdown();
        _tokenSource?.Dispose();
    }
}