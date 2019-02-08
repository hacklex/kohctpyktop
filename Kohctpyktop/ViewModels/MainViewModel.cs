using System;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Kohctpyktop.Input;
using Kohctpyktop.Models;
using Kohctpyktop.Rendering;

namespace Kohctpyktop.ViewModels
{
    public class MainViewModel : IDisposable, INotifyPropertyChanged
    {
        private readonly Renderer _renderer;
        private ImageSource _field;

        public MainViewModel()
        {
            GameModel = new Game();
            InputHandler = new InputHandler(GameModel);
            _renderer = new Renderer(GameModel.Level);

            Redraw();
        }

        public Game GameModel { get; }
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
            RedrawIfChanged();
        }

        private void RedrawIfChanged()
        {
            if (GameModel.IsModelChanged || InputHandler.SelectionState != SelectionState.None) // todo
                Redraw();
            GameModel.ResetChangeMark();
        }

        private void Redraw()
        {
            _renderer.Render(new RenderOpts(InputHandler.SelectionState, InputHandler.Selection));
            
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