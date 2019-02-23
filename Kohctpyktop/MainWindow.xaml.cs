using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
using JsonSubTypes;
using Kohctpyktop.Input;
using Kohctpyktop.Models.Field;
using Kohctpyktop.ViewModels;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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

        private JsonSerializerSettings BuildSerializerSettings()
        {
            var settings = new JsonSerializerSettings();
            
            settings.Converters.Add(new StringEnumConverter());
            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(ValuesFunction), nameof(ValuesFunction.Type))
                .RegisterSubtype(typeof(StaticValuesFunction), ValuesFunctionType.Static)
                .RegisterSubtype(typeof(PeriodicValuesFunction), ValuesFunctionType.Periodic)
                .Build());

            return settings;
        }

        private void OnOpenMenuItemClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() ?? false)
            {
                try
                {
                    using (var file = File.OpenRead(ofd.FileName))
                    using (var gzipStream = new GZipStream(file, CompressionMode.Decompress))
                    using (var reader = new StreamReader(gzipStream))
                    {
                        var json = reader.ReadToEnd();
                        var layerData = JsonConvert.DeserializeObject<LayerData>(json, BuildSerializerSettings());

                        ViewModel.OpenLayer(new Layer(layerData));
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
                var layerData = ViewModel.Layer.ExportLayerData();
                var json = JsonConvert.SerializeObject(layerData, BuildSerializerSettings());

                try
                {
                    using (var file = File.OpenWrite(sfd.FileName))
                    {
                        file.SetLength(0);
                        
                        using (var gzipStream = new GZipStream(file, CompressionMode.Compress))
                        using (var writer = new StreamWriter(gzipStream))
                            writer.Write(json);
                    }
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
