using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Kohctpyktop.Input;
using Kohctpyktop.Models;
using Kohctpyktop.Models.Field;
using Kohctpyktop.Serialization;
using Kohctpyktop.ViewModels;
using Microsoft.Win32;
using Image = System.Windows.Controls.Image;

namespace Kohctpyktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new MainViewModel();
            DataContext = ViewModel;
            SetScaleFactors();
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
            {
                ViewModel.ProcessMouse(pt);
            }
            else
            {
                ViewModel.ReleaseMouse(pt);
                ViewModel.ProcessMouseMove(pt);
            }
        }

        private void ImageMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Released)
                ViewModel.ReleaseMouse(e.GetPosition((Image)sender));
        }

        private void WindowKeyUp(object sender, KeyEventArgs e)
        {
            ViewModel.SetCtrlState(e.KeyboardDevice.IsKeyDown(Key.LeftCtrl));
            
            switch (e.Key)
            {
                case Key.LeftShift:
                    ViewModel.SetShiftState(false);
                    break;
            }
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            ViewModel.SetCtrlState(e.KeyboardDevice.IsKeyDown(Key.LeftCtrl));
            
            switch (e.Key)
            {
                case Key.Z when e.KeyboardDevice.IsKeyDown(Key.LeftCtrl):
                    if (e.KeyboardDevice.IsKeyDown(Key.LeftShift))
                        ViewModel.Redo();
                    else
                        ViewModel.Undo();
                    break;
                case Key.LeftShift:
                    ViewModel.SetShiftState(true);
                    break;
                case Key.D1:
                    ViewModel.SelectTool(SelectedTool.Silicon);
                    break;
                case Key.D2:
                    ViewModel.SelectTool(SelectedTool.Metal);
                    break;
                case Key.D3:
                    ViewModel.SelectTool(SelectedTool.AddOrDeleteVia);
                    break;
                case Key.D4:
                    ViewModel.SelectTool(SelectedTool.Selection);
                    break;
                case Key.D5:
                    ViewModel.SelectTool(SelectedTool.DeleteMetalOrSilicon);
                    break;
                case Key.D6:
                    ViewModel.SelectTool(SelectedTool.TopologyDebug);
                    break;
            }
        }

        private void OnOpenMenuItemClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() ?? false)
            {
                try
                {
                    using (var file = File.OpenRead(ofd.FileName))
                    {
                        ViewModel.OpenLayer(LayerSerializer.ReadLayer(file));
                    }
                }
                catch (Exception ex)
                {
                    // todo: handling
                    MessageBox.Show("Failed to open layer data", "Error", MessageBoxButton.OK);
                }
            }
        }

        private void OnSaveMenuItemClick(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            if (sfd.ShowDialog() ?? false)
            {
                try
                {
                    using (var file = File.OpenWrite(sfd.FileName))
                    {
                        file.SetLength(0);
                        LayerSerializer.WriteLayer(file, ViewModel.Layer);
                    }
                }
                catch (Exception ex)
                {
                    // todo: handling
                    MessageBox.Show("Failed to save layer data", "Error", MessageBoxButton.OK);
                }
            }
        }
        
        private void OnOpenTemplateMenuItemClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() ?? false)
            {
                try
                {
                    using (var file = File.OpenRead(ofd.FileName))
                    {
                        ViewModel.OpenLayer(new Layer(LayerSerializer.ReadTemplate(file)));
                    }
                }
                catch (Exception ex)
                {
                    // todo: handling
                    MessageBox.Show("Failed to open layer data", "Error", MessageBoxButton.OK);
                }
            }
        }

        private void StartSimulation(object sender, RoutedEventArgs e)
        {
            ViewModel.Simulate();
        }

        private void OpenTemplateEditor(object sender, RoutedEventArgs e)
        {
            var window = new TemplateEditWindow(new TemplateEditViewModel(ViewModel.Layer.Template));
            window.ShowDialog();
        }

        public static double XDpiFactor = 1;
        public static double YDpiFactor = 1;

        private void OnWindowDpiChanged(object sender, DpiChangedEventArgs e)
        {
            SetScaleFactors();
        }

        private void SetScaleFactors()
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                Position.DpiFactorX = g.DpiX / 96.0;
                Position.DpiFactorY = g.DpiY / 96.0;
            }
        }
    }
}
