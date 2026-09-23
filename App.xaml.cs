using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace ThreeMFExplorer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private static string CrashLogPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3MF-Explorer",
            "crash.log");

    private static void WriteCrashLog(string source, Exception? exception)
    {
        try
        {
            string? directory = Path.GetDirectoryName(CrashLogPath);

            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.AppendAllText(
                CrashLogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}{Environment.NewLine}" +
                $"{exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    private void App_DispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        WriteCrashLog(
            "DispatcherUnhandledException",
            e.Exception);

        MessageBox.Show(
            $"3MF-Explorer ist auf einen unerwarteten Fehler gestoßen.\n\n" +
            $"{e.Exception.Message}\n\n" +
            $"Crash-Log:\n{CrashLogPath}",
            "3MF-Explorer",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }

    private static void CurrentDomain_UnhandledException(
        object sender,
        UnhandledExceptionEventArgs e)
    {
        WriteCrashLog(
            "AppDomain.UnhandledException",
            e.ExceptionObject as Exception);
    }
}
