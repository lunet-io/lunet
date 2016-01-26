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

            var site = SiteObject.FromFile(Path.Combine(Environment.CurrentDirectory, args[0]), loggerFactory);
            //site.Initialize();


            site.Generator.Run();
        }
    }
}
