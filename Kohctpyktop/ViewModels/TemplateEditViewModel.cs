using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Kohctpyktop.Models;
using Kohctpyktop.Models.Field;

namespace Kohctpyktop.ViewModels
{
    public interface ICanvasObject
    {
        int X { get; set; }
        int Y { get; set; }
        
        int Width { get; set; }
        int Height { get; set; }
    }
    
    public class PinTemplate : ICanvasObject, INotifyPropertyChanged
    {
        public int X { get; set; }
        public int Y { get; set; }
        
        public int Width { get; set; }
        public int Height { get; set; }
        
        public string Name { get; set; }
        
        public event PropertyChangedEventHandler PropertyChanged;
    }
    
    public class DeadZoneTemplate : ICanvasObject, INotifyPropertyChanged
    {
        public int X { get; set; }
        public int Y { get; set; }
        
        public int Width { get; set; }
        public int Height { get; set; }
        
        public event PropertyChangedEventHandler PropertyChanged;
    }
    
    public class TemplateEditViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<PinTemplate> Pins { get; } = new ObservableCollection<PinTemplate>
        {
            new PinTemplate
            {
                X = 2,
                Y = 2,
                Height = 3,
                Width = 3,
                Name = "+VCC"
            }
        };
        
        public ObservableCollection<DeadZoneTemplate> DeadZones { get; } = new ObservableCollection<DeadZoneTemplate>
        {
            new DeadZoneTemplate {
                X = 0,
                Y = 0,
                Width = 2,
                Height = 2
            },
            new DeadZoneTemplate {
                X = 4,
                Y = 6,
                Width = 5,
                Height = 3
            }
        };

        public int Width { get; set; } = 15;
        public int Height { get; set; } = 15;

        public void Move(ICanvasObject obj, int x, int y)
        {
            obj.X = Math.Min(Math.Max(x, 0), Width - obj.Width);
            obj.Y = Math.Min(Math.Max(y, 0), Height - obj.Height);
        }
        
        public void Resize(ICanvasObject obj, int x, int y, int w, int h)
        {
            var preW = Math.Max(1, Math.Min(w, Width - Math.Max(0, x)));
            var preH = Math.Max(1, Math.Min(h, Height - Math.Max(0, y)));
            
            var realX = Math.Min(Math.Max(x, 0), Width - preW);
            var realY = Math.Min(Math.Max(y, 0), Height - preH);
            
            w += x - realX;
            h += y - realY;

            obj.X = realX;
            obj.Y = realY;
            obj.Width = Math.Max(1, Math.Min(w, Width - realX));
            obj.Height = Math.Max(1, Math.Min(h, Height - realY));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}