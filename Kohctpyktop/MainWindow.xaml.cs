using System;
using System.Collections.Generic;
using System.IO;
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
using Microsoft.Win32;
using Newtonsoft.Json;

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
                    var json = File.ReadAllText(ofd.FileName);
                    var layerData = JsonConvert.DeserializeObject<LayerData>(json);

                    ViewModel.OpenLayer(new Layer(layerData));
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
                var layerData = ViewModel.Layer.ExportLayerData();
                var json = JsonConvert.SerializeObject(layerData);

                try
                {
                    File.WriteAllText(sfd.FileName, json);
                }
                catch (Exception ex)
                {
                    // todo: handling
                    MessageBox.Show("Failed to save layer data", "Error", MessageBoxButton.OK);
                }
            }
        }

        private void StartSimulation(object sender, RoutedEventArgs e)
        {
            ViewModel.Simulate();
        }
    }
}
