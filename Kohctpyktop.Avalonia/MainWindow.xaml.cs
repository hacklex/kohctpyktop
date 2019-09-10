using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Kohctpyktop.Avalonia.ViewModels;

namespace Kohctpyktop.Avalonia
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            const uint sz = 50;
            
            var field = new Field(sz, sz);
            var rand = new Random(1337);
            
            for (var i = 0u; i < sz; i++)
            for (var j = 0u; j < sz; j++)
            {
                field.Cells[i, j] = new Cell(rand.Next(2) == 0);
                
                field.LeftToRightLinks[i, j] = new Link(rand.Next(2) == 0);
                field.TopToBottomLinks[i, j] = new Link(rand.Next(2) == 0);
            }
            
            DataContext = new CellFieldViewModel(field);
            
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
