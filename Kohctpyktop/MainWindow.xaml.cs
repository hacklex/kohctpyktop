using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Kohctpyktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            var gameInputHandler = new Game();
            InputHandler = new InputHandler(gameInputHandler);
            DataContext = InputHandler;
        }
        
        public InputHandler InputHandler { get; }

        private void TestClick(object sender, RoutedEventArgs e)
        {
            var lvl = new Game(Level.CreateDummy());
        }

        private void ImageMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
                InputHandler.ProcessMouse(e.GetPosition((Image) sender));
        }

        private void ImageMouseMove(object sender, MouseEventArgs e)
        {
            var pt = e.GetPosition((Image) sender);
            if (e.LeftButton == MouseButtonState.Pressed)
                InputHandler.ProcessMouse(pt);
            InputHandler.ProcessMouseMove(pt);
        }

        private void ImageMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Released)
                InputHandler.ReleaseMouse(e.GetPosition((Image)sender));
        }

        private void WindowKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift && InputHandler != null)
                InputHandler.IsShiftPressed = false;
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftShift:
                    InputHandler.IsShiftPressed = true;
                    break;
                case Key.D1:
                    InputHandler.SelectedTool = SelectedTool.Silicon;
                    break;
                case Key.D2:
                    InputHandler.SelectedTool = SelectedTool.Metal;
                    break;
                case Key.D3:
                    InputHandler.SelectedTool = SelectedTool.AddOrDeleteVia;
                    break;
                // todo: selection
                case Key.D5:
                    InputHandler.SelectedTool = SelectedTool.DeleteMetalOrSilicon;
                    break;
                case Key.D6:
                    InputHandler.SelectedTool = SelectedTool.TopologyDebug;
                    break;
            }
        }
    }
}
