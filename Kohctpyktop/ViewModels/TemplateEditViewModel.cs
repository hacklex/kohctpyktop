using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Kohctpyktop.Models.Field.ValuesFunctions;
using PropertyChanged;

namespace Kohctpyktop.ViewModels
{
    public interface ICanvasObject
    {
        int X { get; set; }
        int Y { get; set; }
        
        int Width { get; set; }
        int Height { get; set; }
    }
    
    public class SequencePartTemplate : INotifyPropertyChanged
    {
        public bool Value { get; set; }
        public int Length { get; set; }
        
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class ValuesFunctionTemplate : INotifyPropertyChanged
    {
        [AlsoNotifyFor(nameof(DisplayName))]
        public ValuesFunctionType Type { get; set; }
        
        // static
        [AlsoNotifyFor(nameof(DisplayName))]
        public bool StaticValue { get; set; }
        
        // periodic
        [AlsoNotifyFor(nameof(DisplayName))]
        public int PeriodicOn { get; set; }
        [AlsoNotifyFor(nameof(DisplayName))]
        public int PeriodicOff { get; set; }
        [AlsoNotifyFor(nameof(DisplayName))]
        public int PeriodicSkip { get; set; }
        
        // sequential
        public ObservableCollection<SequencePartTemplate> Sequence { get; } = new ObservableCollection<SequencePartTemplate>();
        
        // aggregate
        [AlsoNotifyFor(nameof(DisplayName))]
        public AggregateOperation AggregateOperation { get; set; }
        public ObservableCollection<ValuesFunctionTemplate> AggregateParts { get; } = new ObservableCollection<ValuesFunctionTemplate>();

        public string DisplayName
        {
            get
            {
                switch (Type)
                {
                    case ValuesFunctionType.Static: return $"Static - {StaticValue}";
                    case ValuesFunctionType.Periodic:
                        return $"Periodic - {PeriodicOn}:{PeriodicOff} (skip {PeriodicSkip})";
                    case ValuesFunctionType.RepeatingSequence: return "Sequence";
                    case ValuesFunctionType.Aggregate:
                        return $"Aggregate ({AggregateOperation})";
                }

                return null;
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
    }
    
    public class PinTemplate : ICanvasObject, INotifyPropertyChanged
    {
        [AlsoNotifyFor(nameof(DisplayName))]
        public int X { get; set; }
        [AlsoNotifyFor(nameof(DisplayName))]
        public int Y { get; set; }
        
        public int Width { get; set; }
        public int Height { get; set; }
        
        [AlsoNotifyFor(nameof(DisplayName))]
        public string Name { get; set; }
        
        public event PropertyChangedEventHandler PropertyChanged;

        public string DisplayName => $"{Name} ({X}:{Y})";

        public ValuesFunctionTemplate ValuesFunction { get; } = new ValuesFunctionTemplate();
    }
    
    public class DeadZoneTemplate : ICanvasObject, INotifyPropertyChanged
    {
        [AlsoNotifyFor(nameof(DisplayName))]
        public int X { get; set; }
        [AlsoNotifyFor(nameof(DisplayName))]
        public int Y { get; set; }
        
        [AlsoNotifyFor(nameof(DisplayName))]
        public int Width { get; set; }
        [AlsoNotifyFor(nameof(DisplayName))]
        public int Height { get; set; }
        
        public event PropertyChangedEventHandler PropertyChanged;

        public string DisplayName => $"{X}:{Y} - {X + Width - 1}:{Y + Height - 1}";
    }
    
    public class TemplateEditViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<PinTemplate> Pins { get; } = new ObservableCollection<PinTemplate>();

        public ObservableCollection<DeadZoneTemplate> DeadZones { get; } = new ObservableCollection<DeadZoneTemplate>();

        public int Width { get; set; } = 27;
        public int Height { get; set; } = 27;
        
        public object SelectedObject { get; set; }

        public void Move(ICanvasObject obj, int x, int y)
        {
            obj.X = Math.Min(Math.Max(x, 0), Width - obj.Width);
            obj.Y = Math.Min(Math.Max(y, 0), Height - obj.Height);
        }
        
        public void Resize(ICanvasObject obj, int x, int y, int w, int h)
        {
            if (w < 1 || h < 1) return;
            
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

        public void AddPin()
        {
            Pins.Add(new PinTemplate { Name = "NEW " + Pins.Count, Width = 3, Height = 3 });
        }
        
        public void AddDeadZone()
        {
            DeadZones.Add(new DeadZoneTemplate { Width = 3, Height = 3 });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RemoveSelectedObject()
        {
            switch (SelectedObject)
            {
                case PinTemplate pt:
                    Pins.Remove(pt);
                    break;
                case DeadZoneTemplate dzt:
                    DeadZones.Remove(dzt);
                    break;
            }
        }
    }
}