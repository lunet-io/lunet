// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Lunet.Core
{
    public class SiteRunner : IDisposable
    {
        private readonly List<ISiteService> _services;

        public SiteRunner(SiteConfiguration config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _services = new List<ISiteService>();
            CommandRunners = new List<ISiteCommandRunner>();
            CommandRunners.AddRange(Config.CommandRunners);
        }

        public SiteConfiguration Config { get; }

        public IReadOnlyCollection<ISiteService> Services => _services;

        public List<ISiteCommandRunner> CommandRunners { get; }

        public TService GetService<TService>() where TService : class, ISiteService
        {
            return _services.OfType<TService>().FirstOrDefault();
        }

        public void RegisterService(ISiteService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (!_services.Contains(service))
            {
                _services.Add(service);
            }
        }
        
        public SiteObject CurrentSite { get; private set; }
        
        public int Run()
        {
            if (CommandRunners.Count == 0) return 0;

            // Token source
            var runnerToken = new CancellationTokenSource();
            var shutdownEvent = new ManualResetEventSlim(false);
            using ConsoleLifetime consoleLifetime = new ConsoleLifetime(Config, runnerToken, shutdownEvent, "Lunet is shutting down.");

            var result = RunnerResult.Exit;
            try
            {
                CurrentSite = new SiteObject(Config);
                while (CurrentSite != null)
                {
                    result = RunnerResult.Exit;

                    foreach (var runner in CommandRunners)
                    {
                        result = runner.Run(this, runnerToken.Token);
                        if (result != RunnerResult.Continue)
                        {
                            break;
                        }
                    }

                    if (result != RunnerResult.Continue)
                    {
                        break;
                    }

                    CurrentSite = CurrentSite.Clone();
                }

                consoleLifetime.SetExitedGracefully();
            }
            finally
            {
                shutdownEvent.Set();
            }

            return result == RunnerResult.ExitWithError ? 1 : 0;
        }

        public void Dispose()
        {
            foreach (var service in Services)
            {
                service.Dispose();
            }
        }
        
        internal class ConsoleLifetime : IDisposable
        {
            private readonly ISiteLoggerProvider _loggerProvider;
            private readonly CancellationTokenSource _cts;
            private readonly ManualResetEventSlim _resetEvent;
            private readonly string _shutdownMessage;
            private bool _disposed;
            private bool _exitedGracefully;

            public ConsoleLifetime(
                ISiteLoggerProvider loggerProvider,
                CancellationTokenSource cts,
                ManualResetEventSlim resetEvent,
                string shutdownMessage)
            {
                this._loggerProvider = loggerProvider;
                this._cts = cts;
                this._resetEvent = resetEvent;
                this._shutdownMessage = shutdownMessage;
                AppDomain.CurrentDomain.ProcessExit += new EventHandler(this.ProcessExit);
                Console.CancelKeyPress += new ConsoleCancelEventHandler(this.CancelKeyPress);
            }

            internal void SetExitedGracefully() => this._exitedGracefully = true;

            public void Dispose()
            {
                if (this._disposed)
                    return;
                this._disposed = true;
                AppDomain.CurrentDomain.ProcessExit -= new EventHandler(this.ProcessExit);
                Console.CancelKeyPress -= new ConsoleCancelEventHandler(this.CancelKeyPress);
            }

            private void CancelKeyPress(object sender, ConsoleCancelEventArgs eventArgs)
            {
                this.Shutdown();
                eventArgs.Cancel = true;
            }

            private void ProcessExit(object sender, EventArgs eventArgs)
            {
                this.Shutdown();
                if (!this._exitedGracefully)
                    return;
                Environment.ExitCode = 0;
            }

            private void Shutdown()
            {
                try
                {
                    if (!this._cts.IsCancellationRequested)
                    {
                        if (!string.IsNullOrEmpty(this._shutdownMessage))
                            _loggerProvider.Info(_shutdownMessage);
                        this._cts.Cancel();
                    }
                }
                catch (Exception ex)
                {
                }
                this._resetEvent.Wait();
            }
        }
    }
}