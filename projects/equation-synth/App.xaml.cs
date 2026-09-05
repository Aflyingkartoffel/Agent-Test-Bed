using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace EquationSynth;

public partial class App : Application
{
    static readonly string StartupLogPath = Path.Combine(Path.GetTempPath(), "equation-synth-startup.log");

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException("Dispatcher exception", e.Exception);
        MessageBox.Show($"Equation Synth could not continue:\n\n{e.Exception.Message}\n\nDetails were written to:\n{StartupLogPath}", "Equation Synth startup error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
        Shutdown(-1);
    }

    static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception) LogException("Unhandled exception", exception);
    }

    static void LogException(string context, Exception exception)
    {
        try
        {
            var details = new StringBuilder()
                .AppendLine($"[{DateTime.Now:O}] {context}")
                .AppendLine(exception.ToString())
                .AppendLine();
            File.AppendAllText(StartupLogPath, details.ToString());
            Debug.WriteLine(details.ToString());
            Console.Error.WriteLine(details.ToString());
        }
        catch
        {
            // Diagnostics must never replace the original application failure.
        }
    }
}
