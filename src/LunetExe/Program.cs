using System;
using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace Lunet
{
    class Program
    {
        static void Main(string[] args)
        {
            var loggerFactory = new LoggerFactory().AddConsole(LogLevel.Trace);

            var site = SiteFactory.FromFile(Path.Combine(Environment.CurrentDirectory, args[0]), loggerFactory);

            site.Initialize();

            //site.Resources.TryInstall("jquery", "2.2.3");


            //site.Generate();

            //site.Statistics.Dump((s => site.Info(s)));
        }
    }
}
