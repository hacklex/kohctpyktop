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
        }

        private Game Model => DataContext as Game;

        private void TestClick(object sender, RoutedEventArgs e)
        {
            var lvl = new Game(Level.CreateDummy());
        }

        private void ImageMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
                Model?.ProcessMouse(e.GetPosition((Image) sender));
        }

        private void ImageMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                Model?.ProcessMouse(e.GetPosition((Image) sender));
        }

        private void ImageMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Released)
                Model?.ReleaseMouse(e.GetPosition((Image)sender));
        }

        private void WindowKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift && Model != null)
                Model.IsShiftPressed = false;
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            if (Model is Game model)
            {
                switch (e.Key)
                {
                    case Key.LeftShift:
                        model.IsShiftPressed = true;
                        break;
                    case Key.D1:
                        model.SelectedTool = SelectedTool.Silicon;
                        break;
                    case Key.D2:
                        model.SelectedTool = SelectedTool.Metal;
                        break;
                    case Key.D3:
                        model.SelectedTool = SelectedTool.AddOrDeleteVia;
                        break;
                    // todo: selection
                    case Key.D5:
                        model.SelectedTool = SelectedTool.DeleteMetalOrSilicon;
                        break;
                }
            }
        }
    }
}
