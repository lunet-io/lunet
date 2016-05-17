using System;
using System.IO;
using Lunet.Core;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Lunet
{
    class Program
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseFileServer(new FileServerOptions());
        }

        static void Main(string[] args)
        {
            var loggerFactory = new LoggerFactory().AddConsole(LogLevel.Trace);

            var site = SiteFactory.FromFile(Path.Combine(Environment.CurrentDirectory, args[0]), loggerFactory);
            site.Generate();

            site.Statistics.Dump((s => site.Info(s)));

            var host = new WebHostBuilder()
                //.UseServer("Microsoft.AspNetCore.Server.WebListener")
                //.UseLoggerFactory(loggerFactory)
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                .UseContentRoot(site.OutputDirectory)
                .UseWebRoot(site.OutputDirectory)
                .UseUrls("http://localhost:5001")
                .UseStartup<Program>()
                .Build();
            host.Run();
        }
    }
}
