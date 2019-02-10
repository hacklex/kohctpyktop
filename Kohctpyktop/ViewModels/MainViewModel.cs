using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Kohctpyktop.Input;
using Kohctpyktop.Models;
using Kohctpyktop.Models.Field;
using Kohctpyktop.Rendering;
using Point = System.Windows.Point;

namespace Kohctpyktop.ViewModels
{
    public class MainViewModel : IDisposable, INotifyPropertyChanged
    {
        private readonly Renderer _renderer;
        private ImageSource _field;

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

            var rightPinCol = layer.Width - 3;
            var powerPins = new[]
            {
                new Pin { Col = 2, Row = 3, Name = "+VCC"},
                new Pin { Col = 2, Row = 23, Name = "+VCC" },
                new Pin { Col = rightPinCol, Row = 3, Name = "+VCC" },
                new Pin { Col = rightPinCol, Row = 23, Name = "+VCC" },
            };
            var dataPins = new[]
            {
                new Pin { Col = 2, Row = 7, Name = "A0" },
                new Pin { Col = 2, Row = 11, Name = "A1" },
                new Pin { Col = 2, Row = 15, Name = "A2" },
                new Pin { Col = 2, Row = 19, Name = "A3" },
                new Pin { Col = rightPinCol, Row = 7, Name = "B0" },
                new Pin { Col = rightPinCol, Row = 11, Name = "B1" },
                new Pin { Col = rightPinCol, Row = 15, Name = "B2" },
                new Pin { Col = rightPinCol, Row = 19, Name = "B3" },
            };

            var pins = dataPins.Concat(powerPins);

            foreach (var pin in pins)
            {
                var pos = new Position(pin.Col, pin.Row);
                BuildPin(pos);
                layer.SetCellName(pos, pin.Name);
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
            Layer = new Layer(30, 27);
            InitLayer(Layer);
            
            InputHandler = new InputHandler(Layer);
            _renderer = new Renderer(Layer);

            Redraw();
        }

        public ILayer Layer { get; }
        public InputHandler InputHandler { get; }

        public ImageSource Field
        {
            get => _field;
            set
            {
                if (_field == value) return;
                
                _field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Field)));
            }
        }

        public void ProcessMouse(Point position)
        {
            InputHandler.ProcessMouse(position);
            RedrawIfChanged();
        }

        public void ProcessMouseMove(Point position)
        {
            InputHandler.ProcessMouseMove(position);
            RedrawIfChanged();
        }

        public void ReleaseMouse(Point position)
        {
            InputHandler.ReleaseMouse(position);
            Layer.CommitChanges();
            RedrawIfChanged();
        }

        public void SelectTool(SelectedTool tool)
        {
            var prevTool = InputHandler.SelectedTool;
            InputHandler.SelectedTool = tool;
            
            if (prevTool == SelectedTool.Selection) Redraw();
        }

        public void SetShiftState(bool shiftPressed) => InputHandler.IsShiftPressed = shiftPressed;
        public void SetAltState(bool altPressed) => InputHandler.IsAltPressed = altPressed;

        private void RedrawIfChanged()
        {
//            if (GameModel.IsModelChanged || InputHandler.SelectionState != SelectionState.None) // todo
                Redraw();
//            GameModel.ResetChangeMark();
        }

        public void Undo()
        {
            if (Layer.CanUndo)
            {
                Layer.Undo();
                Redraw();
            }
        }

        private void Redraw()
        {
            _renderer.Render(new RenderOpts(InputHandler.SelectionState, InputHandler.Selection, InputHandler._assignments, InputHandler.HoveredNode));
            
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
    }
}