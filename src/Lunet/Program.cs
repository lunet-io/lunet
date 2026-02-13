// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Lunet.Core;

namespace Lunet;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var config = new SiteConfiguration();
        if (args.Any(x => x == "--profiler"))
        {
            // Remove --profiler arg
            args = args.Where(x => x != "--profiler").ToArray();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var profiler = new SuperluminalProfiler();
                config.Profiler = profiler;
            }
        }

        var app = new LunetApp(config);
        return await app.RunAsync(args);
    }

    private class SuperluminalProfiler : IProfiler
    {
        public SuperluminalProfiler()
        {
            SuperluminalPerf.Initialize();
        }

        public void BeginEvent(string name, string data, ProfilerColor color)
        {
            SuperluminalPerf.BeginEvent(name, data, new SuperluminalPerf.ProfilerColor(color.Value));
        }

        public void EndEvent()
        {
            SuperluminalPerf.EndEvent();
        }
    }
}