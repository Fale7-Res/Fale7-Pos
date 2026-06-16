using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Fale7_POS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static int _fatalHandlerGuard;

        private static string PosFatalLogPath
        {
            get
            {
                var dir = Path.Combine(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory), "logs");
                try
                {
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                }
                catch
                {
                }
                return Path.Combine(dir, "pos_fatal.log");
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var message = e == null || e.Exception == null ? "Unknown UI error." : e.Exception.Message;
            AppendFatalLog("DispatcherUnhandledException", e == null || e.Exception == null ? "null exception" : e.Exception.ToString());
            try
            {
                MessageBox.Show("Unexpected error: " + message, "Fale7 POS", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
            }
            if (e != null) e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e == null ? null : e.ExceptionObject as Exception;
            var message = ex == null ? "Unknown fatal error." : ex.Message;
            AppendFatalLog("CurrentDomain_UnhandledException", ex == null ? message : ex.ToString());

            // Important: do not show MessageBox directly on non-UI threads.
            if (Interlocked.Exchange(ref _fatalHandlerGuard, 1) == 1) return;
            try
            {
                var dispatcher = Current != null ? Current.Dispatcher : null;
                if (dispatcher != null && !dispatcher.HasShutdownStarted)
                {
                    dispatcher.BeginInvoke(new Action(delegate
                    {
                        try
                        {
                            MessageBox.Show("Fatal error: " + message, "Fale7 POS", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        catch
                        {
                        }
                    }), DispatcherPriority.Background);
                }
            }
            catch
            {
            }
            finally
            {
                Interlocked.Exchange(ref _fatalHandlerGuard, 0);
            }
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            var message = e == null || e.Exception == null ? "Background task error." : e.Exception.GetBaseException().Message;
            AppendFatalLog("TaskScheduler_UnobservedTaskException", e == null || e.Exception == null ? message : e.Exception.ToString());
            if (e != null) e.SetObserved();
        }

        private static void AppendFatalLog(string scope, string details)
        {
            try
            {
                var line =
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") +
                    " [" + (scope ?? "unknown") + "] " +
                    (details ?? string.Empty);
                File.AppendAllText(PosFatalLogPath, line + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
