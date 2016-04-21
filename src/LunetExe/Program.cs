using System;
using System.IO;
using System.IO.Compression;
using Lunet.Core;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Lunet
{
    class Program
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"C:\Code\lunet-io\lunet\doc\_site")                
            });

            //app.UseStaticFiles(new StaticFileOptions()
            //{
            //    FileProvider = new PhysicalFileProvider(@"C:\Code\lunet-io\lunet\doc\_site")
            //});
        }


        static void Main(string[] args)
        {
            var loggerFactory = new LoggerFactory().AddConsole(LogLevel.Trace);

            var site = SiteFactory.FromFile(Path.Combine(Environment.CurrentDirectory, args[0]), loggerFactory);

            //site.Initialize();

            //site.Resources.TryInstall("jquery", "2.2.3");


            site.Generate();

            site.Statistics.Dump((s => site.Info(s)));


            //var webListener = new WebListener(loggerFactory);
            //webListener.Start();

            var directoryName = Path.GetDirectoryName(Path.Combine(Environment.CurrentDirectory, args[0]));

            //WebListener webListener = new WebListener(loggerFactory);
            //FeatureCollection features = new FeatureCollection();
            //features.Set<WebListener>(webListener);
            //features.Set<MessagePump>(new MessagePump(webListener, this._loggerFactory));
            //features.Set<IServerAddressesFeature>(expr_11, this.SplitAddresses(configuration));
            var host = new WebHostBuilder()
                .UseServer("Microsoft.AspNetCore.Server.WebListener")
                .UseContentRoot(directoryName)
                .UseWebRoot("_site")
                .UseUrls("http://localhost:5001")
                .UseStartup<Program>()
                .Build();
            host.Run();
            //Microsoft.AspNet.Hosting.WebApplication
        }
    }
}
