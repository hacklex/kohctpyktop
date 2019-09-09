using System;
using Avalonia;
using Avalonia.Logging.Serilog;
using Avalonia.Rendering;

namespace Kohctpyktop.Avalonia
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            var builder = AppBuilder.Configure<App>()
                .With(new Win32PlatformOptions
                {
                    AllowEglInitialization = true
                })
                .UsePlatformDetect();
            var wp = builder.WindowingSubsystemInitializer;
            return builder.UseWindowingSubsystem(() =>
            {
                wp();
                AvaloniaLocator.CurrentMutable.Bind<IRenderTimer>().ToConstant(new DefaultRenderTimer(500));
            });
        }

    }
}
