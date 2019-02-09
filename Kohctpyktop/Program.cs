using Avalonia;
using Avalonia.Logging.Serilog;
using Serilog;

namespace Kohctpyktop
{
    class Program
    {
        static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .CreateLogger();

            BuildAvaloniaApp().Start<MainWindow>();
        }

        static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UseDirect2D1()
                .UseWin32()
                .LogToTrace();
    }
}