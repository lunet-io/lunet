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


            DumpDependencies(site, "statics", site.StaticFiles);
            DumpDependencies(site, "dynamics", site.DynamicPages);
            DumpDependencies(site, "pages", site.Pages);

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
