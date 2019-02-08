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
using Kohctpyktop.Input;
using Kohctpyktop.Models.Field;
using Kohctpyktop.ViewModels;

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

            ViewModel = new MainViewModel();
            DataContext = ViewModel;
        }

        public MainViewModel ViewModel { get; }

        private void ImageMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
                ViewModel.ProcessMouse(e.GetPosition((Image) sender));
        }

        private void ImageMouseMove(object sender, MouseEventArgs e)
        {
            var pt = e.GetPosition((Image) sender);
            if (e.LeftButton == MouseButtonState.Pressed)
                ViewModel.ProcessMouse(pt);
            ViewModel.ProcessMouseMove(pt);
        }

        private void ImageMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Released)
                ViewModel.ReleaseMouse(e.GetPosition((Image)sender));
        }

        private void WindowKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift)
                ViewModel.InputHandler.IsShiftPressed = false;
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftShift:
                    ViewModel.InputHandler.IsShiftPressed = true;
                    break;
                case Key.D1:
                    ViewModel.InputHandler.SelectedTool = SelectedTool.Silicon;
                    break;
                case Key.D2:
                    ViewModel.InputHandler.SelectedTool = SelectedTool.Metal;
                    break;
                case Key.D3:
                    ViewModel.InputHandler.SelectedTool = SelectedTool.AddOrDeleteVia;
                    break;
                case Key.D4:
                    ViewModel.InputHandler.SelectedTool = SelectedTool.Selection;
                    break;
                case Key.D5:
                    ViewModel.InputHandler.SelectedTool = SelectedTool.DeleteMetalOrSilicon;
                    break;
                case Key.D6:
                    ViewModel.InputHandler.SelectedTool = SelectedTool.TopologyDebug;
                    break;
            }
        }
    }
}
