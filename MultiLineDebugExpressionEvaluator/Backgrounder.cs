using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Threading;
using Events = EnvDTE.Events;
using SolutionEvents = EnvDTE.SolutionEvents;

namespace MultiLineDebugExpressionEvaluator
{
    public class Backgrounder
    {
        private readonly CancellationTokenSource _cts;

        private readonly Events _events;
        private readonly DTEEvents _dteEvents;
        private readonly SolutionEvents _solutionEvents; //do not remove even if it looks unusable!

        public Backgrounder(DTE2 dte)
        {
            if (dte is null)
            {
                throw new ArgumentNullException(nameof(dte));
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            _cts = new CancellationTokenSource();

            _events = dte.Events;
            _dteEvents = _events.DTEEvents;
            _solutionEvents = _events.SolutionEvents;

            _dteEvents.OnBeginShutdown += () => _cts.Cancel();
        }

        public async Task ScanAsync()
        {
            try
            {
                const int DefaultWaitTimeout = 1000;
                const int IncreaseWaitTimeout = 1000;

                var waitTimeout = DefaultWaitTimeout;

                var disabled = false;

                while (true)
                {
                    try
                    {
                        if (_cts.Token.IsCancellationRequested)
                        {
                            return;
                        }

                        await Task.Delay(waitTimeout, _cts.Token);

                        if (disabled)
                        {
                            continue;
                        }

                        //if (!General.Instance.Enabled)
                        //{
                        //    disabled = true;
                        //    continue;
                        //}

                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(
                            _cts.Token
                            );
                        
                        var dialog = QuickWatchDialog.TryCreate(
                            );
                        if (dialog is null)
                        {
                            continue;
                        }

                        dialog.InitialSetup();

                        await dialog.WaitForClosingAsync(_cts.Token);

                        await TaskScheduler.Default;

                        //disabled = true;

                        //restore timeout if case we're successful
                        waitTimeout = DefaultWaitTimeout;
                    }
                    catch (OperationCanceledException)
                    {
                        //task cancelled
                        return;
                    }
                    catch (Exception excp)
                    {
                        Logging.LogVS(excp.Message);
                        Logging.LogVS(excp.StackTrace);

                        //increase timeout to prevent spam into the log
                        waitTimeout += IncreaseWaitTimeout;
                    }
                }
            }
            catch (Exception excp)
            {
                Logging.LogVS("STOP ERROR");
                Logging.LogVS(excp.Message);
                Logging.LogVS(excp.StackTrace);
            }
        }
    }
}
