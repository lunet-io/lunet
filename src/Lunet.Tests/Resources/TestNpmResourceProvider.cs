// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lunet.Core;
using Lunet.Resources;
using Lunet.Scripts;
using Lunet.Tests.Infrastructure;
using Zio;

namespace Lunet.Tests.Resources;

public class TestNpmResourceProvider
{
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task StalledResponseTimesOutWithoutCreatingCache(bool tarball, bool sendHeaders)
    {
        using var context = new SiteTestContext();
        await using var server = new RegistryServer(async (path, stream, token) =>
        {
            if (tarball && path == "/pkg") return false;
            if (sendHeaders)
            {
                await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 10000\r\n\r\n"), token);
                // Start the body but never finish it.
                await stream.WriteAsync(new byte[] { 0x1f, 0x8b }, token);
            }
            await Task.Delay(Timeout.Infinite, token);
            return true;
        });
        var plugin = new ResourcePlugin(context.Site);
        var provider = (NpmResourceProvider)plugin.Providers[0];
        provider.RegistryUrl = server.Url;

        var install = Task.Run(() =>
        {
            ImportConfig(context, "resources.npm_request_timeout = 1\ninstalled = resource \"npm:pkg\" \"1.0.0\"");
            return context.Site["installed"];
        });
        try
        {
            Assert.Null(await install.WaitAsync(TimeSpan.FromSeconds(5)), "A stalled HTTP response must fail within the request deadline.");
            Assert.IsTrue(context.Configuration.LoggerFactory.HasErrors);
            Assert.IsFalse(context.Site.CacheMetaFileSystem.DirectoryExists("/resources/npm/pkg/1.0.0"));
            Assert.IsEmpty(provider.Resources);
        }
        finally
        {
            // Close the connection even if the regression returns, so the test cannot hang the suite.
            await server.DisposeAsync();
            await install.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [TestCase("1.0.0")]
    [TestCase("latest")]
    public async Task DownloadsAndCachesPackageWithoutCapturingSynchronizationContext(string version)
    {
        using var context = new SiteTestContext();
        await using var server = new RegistryServer();
        var provider = new NpmResourceProvider(new ResourcePlugin(context.Site)) { RegistryUrl = server.Url };
        var synchronizationContext = new RecordingSynchronizationContext();

        var resource = await Task.Run(() =>
        {
            var previous = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(synchronizationContext);
                return provider.GetOrInstall("pkg", version, ResourceInstallFlags.Private);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.NotNull(resource);
        Assert.AreEqual("1.0.0", resource!.Version);
        Assert.AreEqual("asset", new FileEntry(resource.AbsoluteDirectory.FileSystem, resource.Path / "asset.txt").ReadAllText());
        Assert.AreSame(resource, provider.GetOrInstall("pkg", "1.0.0", ResourceInstallFlags.Private));
        Assert.AreEqual(2, server.RequestCount);
        Assert.AreEqual(0, synchronizationContext.PostCount);
        Assert.IsFalse(context.Configuration.LoggerFactory.HasErrors);
    }

    [Test]
    public async Task TruncatedDownloadDoesNotPoisonCacheAndCanBeRetried()
    {
        using var context = new SiteTestContext();
        var truncate = true;
        await using var server = new RegistryServer(async (path, stream, token) =>
        {
            if (path == "/pkg" || !truncate) return false;
            var body = RegistryServer.CreateArchive();
            // Even if the gzip archive is readable, the HTTP transfer is not complete.
            await stream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Length: {body.Length + 100}\r\nConnection: close\r\n\r\n"), token);
            await stream.WriteAsync(body, token);
            return true;
        });
        var provider = new NpmResourceProvider(new ResourcePlugin(context.Site)) { RegistryUrl = server.Url };

        Assert.Null(await Task.Run(() => provider.GetOrInstall("pkg", "1.0.0", ResourceInstallFlags.Private)).WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.IsFalse(context.Site.CacheMetaFileSystem.DirectoryExists("/resources/npm/pkg/1.0.0"));
        Assert.IsEmpty(provider.Resources);

        truncate = false;
        Assert.NotNull(await Task.Run(() => provider.GetOrInstall("pkg", "1.0.0", ResourceInstallFlags.Private)).WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.AreEqual(4, server.RequestCount);
    }

    [Test]
    public void RequestTimeoutMustBePositiveAndFinite()
    {
        using var context = new SiteTestContext();
        var provider = new NpmResourceProvider(new ResourcePlugin(context.Site));

        Assert.AreEqual(TimeSpan.FromSeconds(100), provider.RequestTimeout);
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.RequestTimeout = TimeSpan.Zero);
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.RequestTimeout = Timeout.InfiniteTimeSpan);
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.RequestTimeout = TimeSpan.FromMilliseconds((long)int.MaxValue + 1));
    }

    [TestCase("resources.npm_request_timeout = 300", 300)]
    [TestCase("with resources\n npm_request_timeout = 0.5\nend", 0.5)]
    public void ConfigSetsTimeoutInSeconds(string script, double seconds)
    {
        using var context = new SiteTestContext();
        var plugin = new ResourcePlugin(context.Site);
        var provider = (NpmResourceProvider)plugin.Providers[0];
        var resources = plugin["npm"];

        ImportConfig(context, script);

        Assert.AreEqual(TimeSpan.FromSeconds(seconds), provider.RequestTimeout);
        Assert.AreSame(resources, plugin["npm"], "Configuration must preserve the loaded-resource collection.");
    }

    [Test]
    public void ConfigAndDotNetApiShareTimeoutValue()
    {
        using var context = new SiteTestContext();
        var plugin = new ResourcePlugin(context.Site);
        var provider = (NpmResourceProvider)plugin.Providers[0];

        ImportConfig(context, "configured_timeout = resources.npm_request_timeout");
        Assert.AreEqual(100, context.Site["configured_timeout"]);

        provider.RequestTimeout = TimeSpan.FromMilliseconds(1250);
        ImportConfig(context, "configured_timeout = resources.npm_request_timeout");
        Assert.AreEqual(1.25, context.Site["configured_timeout"]);

        ImportConfig(context, "resources.npm_request_timeout = 200");
        Assert.AreEqual(TimeSpan.FromSeconds(200), provider.RequestTimeout);
    }

    [TestCase("0")]
    [TestCase("-1")]
    [TestCase("2147483.648")]
    [TestCase("null")]
    [TestCase("true")]
    [TestCase("\"300\"")]
    public void InvalidConfiguredTimeoutIsRejected(string value)
    {
        using var context = new SiteTestContext();
        var plugin = new ResourcePlugin(context.Site);
        var provider = (NpmResourceProvider)plugin.Providers[0];

        ImportConfig(context, $"resources.npm_request_timeout = {value}");

        var exception = Assert.Throws<LunetException>(() => _ = provider.RequestTimeout);
        StringAssert.Contains("resources.npm_request_timeout", exception!.Message);
    }

    private static void ImportConfig(SiteTestContext context, string script)
    {
        context.WriteInputFile("/config.scriban", script);
        Assert.IsTrue(context.Site.Scripts.TryImportScriptFromFile(
            new FileEntry(context.Site.SiteFileSystem, "/config.scriban"), context.Site,
            ScriptFlags.Expect | ScriptFlags.AllowSiteFunctions, out _));
    }

    [TestCase(403, false)]
    [TestCase(404, false)]
    [TestCase(429, false)]
    [TestCase(503, false)]
    [TestCase(429, true)]
    public async Task HttpErrorsFailWithoutCreatingCache(int status, bool tarball)
    {
        using var context = new SiteTestContext();
        await using var server = new RegistryServer(async (path, stream, token) =>
        {
            if (tarball && path == "/pkg") return false;
            await stream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 {status} Error\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"), token);
            return true;
        });
        var provider = new NpmResourceProvider(new ResourcePlugin(context.Site)) { RegistryUrl = server.Url };

        Assert.Null(await Task.Run(() => provider.GetOrInstall("pkg", "1.0.0", ResourceInstallFlags.Private)).WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.IsTrue(context.Configuration.LoggerFactory.HasErrors);
        Assert.IsFalse(context.Site.CacheMetaFileSystem.DirectoryExists("/resources/npm/pkg/1.0.0"));
        Assert.AreEqual(tarball ? 2 : 1, server.RequestCount);
    }

    private sealed class RecordingSynchronizationContext : SynchronizationContext
    {
        private int _postCount;

        public int PostCount => _postCount;

        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref _postCount);
            ThreadPool.QueueUserWorkItem(_ => d(state));
        }
    }

    private sealed class RegistryServer : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _stop = new(TimeSpan.FromSeconds(20));
        private readonly Task _serve;
        private bool _disposed;

        public RegistryServer(Func<string, NetworkStream, CancellationToken, Task<bool>>? respond = null)
        {
            _listener.Start();
            Url = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/";
            _serve = ServeAsync(respond);
        }

        public string Url { get; }

        public int RequestCount { get; private set; }

        private async Task ServeAsync(Func<string, NetworkStream, CancellationToken, Task<bool>>? respond)
        {
            var token = _stop.Token;
            try
            {
                while (true)
                {
                    using var client = await _listener.AcceptTcpClientAsync(token);
                    using var stream = client.GetStream();
                    using var reader = new StreamReader(stream, leaveOpen: true);
                    var requestLine = await reader.ReadLineAsync(token);
                    while (await reader.ReadLineAsync(token) is { Length: > 0 }) { }
                    var path = requestLine!.Split(' ')[1];
                    RequestCount++;
                    if (respond is not null && await respond(path, stream, token)) continue;

                    var body = path == "/pkg"
                        ? Encoding.UTF8.GetBytes("""{"versions":{"1.0.0":{"dist":{"tarball":"TARBALL_URL"}}}}""".Replace("TARBALL_URL", Url + "pkg.tgz"))
                        : CreateArchive();
                    await stream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n"), token);
                    await stream.WriteAsync(body, token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
        }

        public static byte[] CreateArchive()
        {
            using var buffer = new MemoryStream();
            using (var gzip = new GZipStream(buffer, CompressionMode.Compress, leaveOpen: true))
            using (var writer = new TarWriter(gzip, leaveOpen: true))
            {
                foreach (var (name, content) in new[] { ("package.json", "{\"name\":\"pkg\",\"version\":\"1.0.0\"}"), ("asset.txt", "asset") })
                {
                    using var data = new MemoryStream(Encoding.UTF8.GetBytes(content));
                    writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, $"package/{name}") { DataStream = data });
                }
            }
            return buffer.ToArray();
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            await _stop.CancelAsync();
            try
            {
                await _serve;
            }
            finally
            {
                _listener.Stop();
                _stop.Dispose();
            }
        }
    }
}
