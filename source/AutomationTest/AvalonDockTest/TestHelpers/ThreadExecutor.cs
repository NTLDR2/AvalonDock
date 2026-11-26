using System;
using System.Linq;
#if !NET452
using System.Runtime.InteropServices;
#endif
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace UnitTests
{
    /// <summary>
    /// Description of ThreadWrapper.
    /// </summary>
    public static class ThreadExecutor
    {
        public static void RunCodeAsSTA(AutoResetEvent are, ThreadStart originalDelegate)
        {
            Thread thread = new Thread(
                delegate()
                {
                    try
                    {
                        originalDelegate.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("An error occurred {0}", ex.Message);
                    }
                    finally
                    {
                        var dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
                        if (dispatcher != null && dispatcher != Application.Current?.Dispatcher)
                        {
                            try
                            {
                                var threadWindows = Application.Current?.Dispatcher?.Invoke(() =>
                                    Application.Current.Windows.OfType<Window>().Where(w => w.Dispatcher == dispatcher).ToList());
                                if (threadWindows != null)
                                {
                                    foreach (var window in threadWindows)
                                    {
                                        try { window.Close(); } catch { }
                                    }
                                }
                            }
                            catch
                            {
                            }

                            if (!dispatcher.HasShutdownStarted && !dispatcher.HasShutdownFinished)
                            {
                                dispatcher.InvokeShutdown();
                            }
                        }

                        are.Set();
                    }
                });
#if !NET452
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                thread.SetApartmentState(ApartmentState.STA);
            }
#else
            // On .NET Framework, we're always on Windows
            thread.SetApartmentState(ApartmentState.STA);
#endif

            thread.Start();
        }
    }
}