using System;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Kohctpyktop.Input;
using Kohctpyktop.Models;
using Kohctpyktop.Models.Field;
using Kohctpyktop.Models.Simulation;
using Kohctpyktop.Models.Topology;
using Kohctpyktop.Rendering;
using Point = System.Windows.Point;

namespace Kohctpyktop.ViewModels
{
    public class MainViewModel : IDisposable, INotifyPropertyChanged
    {
        private Renderer _renderer;
        private ImageSource _field;
        private SimulationResult _simulation;
        private InputHandler _inputHandler;

        private DateTimeOffset _lastOpenTime;

        private static void InitLayer(ILayer layer)
        {
            void BuildPin(Position pos)
            {
                for (var i = 0; i < 3; i++)
                for (var j = 0; j < 3; j++)
                {
                    layer.AddCellMetal(pos.Offset(i - 1, j - 1));
                }
                
                for (var i = 0; i < 2; i++)
                for (var j = 0; j < 2; j++)
                {
                    layer.AddLink(pos.Offset(i - 1, j - 1), pos.Offset(i - 1, j), LinkType.MetalLink);
                    layer.AddLink(pos.Offset(i - 1, j - 1), pos.Offset(i, j - 1), LinkType.MetalLink);
                }
                
                for (var i = 0; i < 2; i++)
                {
                    layer.AddLink(pos.Offset(i - 1, 1), pos.Offset(i, 1), LinkType.MetalLink);
                    layer.AddLink(pos.Offset(1, i - 1), pos.Offset(1, i), LinkType.MetalLink);
                }
            }

            ValuesFunction FlipFlop(int on, int off, int skip = 0)
                => new PeriodicValuesFunction(on, off, skip);
            
            var rightPinCol = layer.Width - 3;
            var powerPins = new[]
            {
                new Pin { Col = 2, Row = 3, Name = "+VCC", ValuesFunction = StaticValuesFunction.AlwaysOn, IsSignificant = false },
                new Pin { Col = 2, Row = 23, Name = "+VCC", ValuesFunction = StaticValuesFunction.AlwaysOn, IsSignificant = false },
                new Pin { Col = rightPinCol, Row = 3, Name = "+VCC", ValuesFunction = StaticValuesFunction.AlwaysOn, IsSignificant = false },
                new Pin { Col = rightPinCol, Row = 23, Name = "+VCC", ValuesFunction = StaticValuesFunction.AlwaysOn, IsSignificant = false },
            };
            var dataPins = new[]
            {
                new Pin { Col = 2, Row = 7, Name = "A0", ValuesFunction = FlipFlop(10, 10) },
                new Pin { Col = 2, Row = 11, Name = "A1", ValuesFunction = FlipFlop(10, 10, 10) },
                new Pin { Col = 2, Row = 15, Name = "A2", ValuesFunction = FlipFlop(20, 10) },
                new Pin { Col = 2, Row = 19, Name = "A3", ValuesFunction = FlipFlop(10, 30) },
                new Pin { Col = rightPinCol, Row = 7, Name = "B0", IsOutputPin = true, ValuesFunction = StaticValuesFunction.AlwaysOff },
                new Pin { Col = rightPinCol, Row = 11, Name = "B1", IsOutputPin = true, ValuesFunction = StaticValuesFunction.AlwaysOff },
                new Pin { Col = rightPinCol, Row = 15, Name = "B2", IsOutputPin = true, ValuesFunction = StaticValuesFunction.AlwaysOff },
                new Pin { Col = rightPinCol, Row = 19, Name = "B3", IsOutputPin = true, ValuesFunction = StaticValuesFunction.AlwaysOff },
            };

            var pins = dataPins.Concat(powerPins);

            foreach (var pin in pins)
            {
                var pos = new Position(pin.Col, pin.Row);
                BuildPin(pos);
                layer.SetCellPin(pos, pin);
            }
            
            for (var i = 0; i < layer.Height; i++)
            for (var j = 0; j < 4; j++)
            {
                layer.SetLockState(new Position(j, i), true);
                layer.SetLockState(new Position(layer.Width - j - 1, i), true);
            }
            
            layer.CommitChanges(false);
        }

        public MainViewModel()
        {
            var layer = new Layer(30, 27);
            InitLayer(layer);
            OpenLayer(layer);
        }

        public void OpenLayer(ILayer layer)
        {
            Layer = layer;
            
            InputHandler = new InputHandler(Layer);
            _renderer?.Dispose();
            _renderer = new Renderer(Layer);

            Simulation = null;

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
        }

        public SimulationResult Simulation
        {
            get => _simulation;
            set { _simulation = value; OnPropertyChanged(); }
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