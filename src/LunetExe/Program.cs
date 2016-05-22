using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lunet.Core;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Lunet
{
    class Program
    {
        private static readonly HashSet<WebSocket> sockets;

        static Program()
        {
            sockets = new HashSet<WebSocket>();
        }

        public Program()
        {
            
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseWebSockets();
            app.Use(HandleWebSockets);

            app.UseFileServer();
        }

        private async Task HandleWebSockets(HttpContext http, Func<Task> next)
        {
            if (http.WebSockets.IsWebSocketRequest)
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

                    await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("test")), WebSocketMessageType.Text, true, CancellationToken.None);
                    while (webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.ReceiveAsync(new ArraySegment<byte>(new byte[1024]), CancellationToken.None);
                    }

                    if (webSocket.State == WebSocketState.CloseReceived)
                    {
                        try
                        {
                            await
                                webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing",
                                    CancellationToken.None);
                        } catch (ObjectDisposedException)
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

        static int Main(string[] args)
        {
            var app = new LunetCommandLine();

            app.OnExecute(() =>
            {
                //RunCommand runCmd = new RunCommand();
                return 0;
            });

            try
            {
                return app.Execute(args);
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.Error.WriteLine(ex.ToString());
#else
                Reporter.Error.WriteLine(ex.Message);
#endif
                return 1;
            }





            return 0;
            var loggerFactory = new LoggerFactory().AddConsole(LogLevel.Trace);

            var watcher = new SiteWatcher(loggerFactory);

            SiteObject site = null;

            var generateSite = new Action(() =>
            {
                site = SiteFactory.FromFile(Path.Combine(Environment.CurrentDirectory, args[0]), loggerFactory);
                site.BaseUrl = "http://localhost:5001";
                site.Generate();
                DumpDependencies(site, "statics", site.StaticFiles);
                DumpDependencies(site, "dynamics", site.DynamicPages);
                DumpDependencies(site, "pages", site.Pages);

                site.Statistics.Dump((s => site.Info(s)));
            });

            generateSite();

            watcher.Start(site);
            watcher.FileSystemEvents += async (sender, eventArgs) =>
            {
                Console.WriteLine($"Received file events [{eventArgs.FileEvents.Count}]");

                // Regenerate website
                generateSite();

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
            };


            var host = new WebHostBuilder()
                .UseLunet(site)
                .Build();

            host.Run();
        }

        private static void DumpDependencies(SiteObject site, string type, PageCollection pages)
        {
            foreach (var page in pages)
            {
                if (page.Discard)
                {
                    continue;
                }

                Console.WriteLine($"Dependency {type} [{page.Path ?? page.Url}]");
                foreach (var dep in page.Dependencies)
                {
                    foreach (var file in dep.GetFiles())
                    {
                        Console.WriteLine($"    -> {file}");
                    }
                }
            }
        }
    }
}
