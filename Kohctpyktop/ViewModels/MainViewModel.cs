using System;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Kohctpyktop.Input;
using Kohctpyktop.Models;
using Kohctpyktop.Models.Field;
using Kohctpyktop.Rendering;

namespace Kohctpyktop.ViewModels
{
    public class MainViewModel : IDisposable, INotifyPropertyChanged
    {
        private readonly Renderer _renderer;
        private ImageSource _field;

        public MainViewModel()
        {
            Layer = new Layer(30, 30);
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