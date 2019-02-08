using Avalonia;
using Avalonia.Markup.Xaml;

namespace Kohctpyktop
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
