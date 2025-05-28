using Avalonia;
using System;
using System.Diagnostics;

namespace StudentManagement;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            Debug.WriteLine("Starting application...");
            var app = BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
            Debug.WriteLine("Application started successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Application failed to start: {ex}");
            Console.WriteLine($"Application failed to start. Error details:");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                Console.WriteLine($"Inner exception stack trace: {ex.InnerException.StackTrace}");
            }
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
