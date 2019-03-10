using System;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Kohctpyktop.Input;
using Kohctpyktop.Models;
using Kohctpyktop.Models.Field;
using Kohctpyktop.Models.Field.ValuesFunctions;
using Kohctpyktop.Models.Simulation;
using Kohctpyktop.Models.Topology;
using Kohctpyktop.Rendering;
using Kohctpyktop.Serialization;
using Point = System.Windows.Point;

namespace Kohctpyktop.ViewModels
{
    public class MainViewModel : IDisposable, INotifyPropertyChanged
    {
        private Renderer _renderer;
        private ImageSource _field;
        private SimulationResult _simulation;
        private InputHandler _inputHandler;
        private bool _isSimulatedOnce;

        private DateTimeOffset _lastOpenTime;

        public MainViewModel()
        {
            var template = LayerSerializer.ReadResourceTemplates().First();
            var layer = new Layer(template);
            OpenLayer(layer);
        }

        public void OpenLayer(ILayer layer)
        {
            Layer = layer;
            
            InputHandler = new InputHandler(Layer);
            _renderer?.Dispose();
            _renderer = new Renderer(Layer);

            Simulate();
            IsSimulatedOnce = false;

            _lastOpenTime = DateTimeOffset.Now;
            Redraw();
        }

        public ILayer Layer { get; private set; }

        public InputHandler InputHandler
        {
            get => _inputHandler;
            private set { _inputHandler = value; OnPropertyChanged(); }
        }

        public ImageSource Field
        {
            get => _field;
            set
            {
                if (Equals(_field, value)) return;
                _field = value;
                OnPropertyChanged();
            }
        }

        public void ProcessMouse(Point position)
        {
            // prevent click after level open
            // doubleclick inside OpenFileDialog causes click over field and single silicon cell draw
            // todo - find better way to work around, or move to view
            if ((DateTime.Now - _lastOpenTime).TotalMilliseconds < 300) return;
            
            if (InputHandler.ProcessMouse(position))
                Redraw();
        }

        public void ProcessMouseMove(Point position)
        {
            if (InputHandler.ProcessMouseMove(position))
                Redraw();
        }

        public void ReleaseMouse(Point position)
        {
            InputHandler.ReleaseMouse(position);
            Layer.CommitChanges();
        }

        public void SelectTool(SelectedTool tool)
        {
            var prevTool = InputHandler.SelectedTool;
            InputHandler.SelectedTool = tool;
            
            if (prevTool == SelectedTool.Selection) Redraw();
        }

        public void SetShiftState(bool shiftPressed) => InputHandler.IsShiftPressed = shiftPressed;
        public void SetCtrlState(bool altPressed) => InputHandler.IsCtrlPressed = altPressed;

        public void Undo()
        {
            if (Layer.CanUndo)
            {
                Layer.Undo();
                Redraw();
            }
        }
        
        public void Redo()
        {
            if (Layer.CanRedo)
            {
                Layer.Redo();
                Redraw();
            }
        }

        public void Simulate()
        {
            var topology = TopologyBuilder.BuildTopology(Layer);
            Simulation = Simulator.Simulate(topology, 100);
            IsSimulatedOnce = true;
        }

        public SimulationResult Simulation
        {
            get => _simulation;
            set { _simulation = value; OnPropertyChanged(); }
        }

        public bool IsSimulatedOnce
        {
            get => _isSimulatedOnce;
            set { _isSimulatedOnce = value; OnPropertyChanged(); }
        }

        private void Redraw()
        {
            _renderer.Render(new RenderOpts(InputHandler.SelectionState, InputHandler.Selection, InputHandler.Assignments, InputHandler.HoveredNode));
            
            var bmpImage = new BitmapImage();
            var stream = new MemoryStream();
            _renderer.Bitmap.Save(stream, ImageFormat.Bmp);
            bmpImage.BeginInit();
            bmpImage.StreamSource = stream;
            bmpImage.EndInit();
            bmpImage.Freeze();
            
            Field = bmpImage;
        }
        
        public void Dispose() => _renderer.Dispose();

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}