using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Kohctpyktop.Models;
using Kohctpyktop.Models.Field;
using Kohctpyktop.Models.Field.ValuesFunctions;
using Kohctpyktop.Models.Templates;
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
        public ValuesFunctionTemplate() {}
        
        public ValuesFunctionTemplate(ValuesFunction func)
        {
            Type = func.Type;
            
            switch (func)
            {
                case StaticValuesFunction svf:
                    StaticValue = svf.Value;
                    break;
                case PeriodicValuesFunction pvf:
                    PeriodicOn = pvf.On;
                    PeriodicOff = pvf.Off;
                    PeriodicSkip = pvf.Skip;
                    break;
                case RepeatingSequenceValuesFunction rsvf:
                    foreach (var part in rsvf.Sequence)
                        Sequence.Add(new SequencePartTemplate { Value = part.Value, Length = part.Length });
                    break;
                case AggregateValuesFunction avf:
                    AggregateOperation = avf.Operation;
                    foreach (var fn in avf.Functions)
                        AggregateParts.Add(new ValuesFunctionTemplate(fn));
                    break;
                case ReferenceValuesFunction rvf:
                    Reference = rvf.Reference;
                    break;
            }
        }

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

        // reference
        [AlsoNotifyFor(nameof(DisplayName))]
        public string Reference { get; set; }
        
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
                    case ValuesFunctionType.Reference:
                        return $"Reference: {Reference}";
                }

                return null;
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        public ValuesFunction Build()
        {
            switch (Type)
            {
                case ValuesFunctionType.Static:
                    return new StaticValuesFunction(StaticValue);
                case ValuesFunctionType.Periodic:
                    return new PeriodicValuesFunction(PeriodicOn, PeriodicOff, PeriodicSkip);
                case ValuesFunctionType.RepeatingSequence:
                    return new RepeatingSequenceValuesFunction(Sequence.Select(x => new SequencePart(x.Value, x.Length)).ToArray());
                case ValuesFunctionType.Aggregate:
                    return new AggregateValuesFunction(AggregateOperation, AggregateParts.Select(x => x.Build()).ToArray());
                case ValuesFunctionType.Reference:
                    return new ReferenceValuesFunction(Reference);
                default:
                    throw new Exception("Unknown type");
            }
        }
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
        
        public bool IsSignificant { get; set; }
        public bool IsOutputPin { get; set; }
        
        public event PropertyChangedEventHandler PropertyChanged;

        public string DisplayName => $"{Name} ({X}:{Y})";

        public ValuesFunctionTemplate ValuesFunction { get; set; } = new ValuesFunctionTemplate();
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

    public class NamedFunctionTemplate : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public ValuesFunctionTemplate Function { get; set; } = new ValuesFunctionTemplate();
        
        public event PropertyChangedEventHandler PropertyChanged;
    }
    
    public class TemplateEditViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<PinTemplate> Pins { get; } = new ObservableCollection<PinTemplate>();

        public ObservableCollection<DeadZoneTemplate> DeadZones { get; } = new ObservableCollection<DeadZoneTemplate>();
        
        public ObservableCollection<NamedFunctionTemplate> Functions { get; } = new ObservableCollection<NamedFunctionTemplate>();

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
        
        public void AddFunction()
        {
            Functions.Add(new NamedFunctionTemplate { Name = "NEW " + Functions.Count });
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

        public void OpenTemplate(LayerTemplate template)
        {
            Width = template.Width;
            Height = template.Height;

            Pins.Clear();
            DeadZones.Clear();

            foreach (var pin in template.Pins)
            {
                Pins.Add(new PinTemplate
                {
                    X = pin.Col,
                    Y = pin.Row,
                    Width = pin.Width,
                    Height = pin.Height,
                    Name = pin.Name,
                    IsSignificant = pin.IsSignificant,
                    IsOutputPin = pin.IsOutputPin,
                    ValuesFunction = new ValuesFunctionTemplate(pin.ValuesFunction)
                });
            }

            foreach (var zone in template.DeadZones)
            {
                DeadZones.Add(new DeadZoneTemplate
                {
                    X = zone.Origin.X,
                    Y = zone.Origin.Y,
                    Width = zone.Width,
                    Height = zone.Height
                });
            }

            foreach (var fun in template.Functions)
            {
                Functions.Add(new NamedFunctionTemplate { Name = fun.Key, Function = new ValuesFunctionTemplate(fun.Value) });
            }
        }

        public LayerTemplate SaveTemplate()
        {
            return new LayerTemplate(Width, Height,
                Pins.Select(x => new Pin(x.Width, x.Height, x.Y, x.X, x.Name, x.ValuesFunction.Build(), x.IsOutputPin, x.IsSignificant)).ToArray(),
                DeadZones.Select(x => new Zone(new Position(x.X, x.Y), x.Width, x.Height)).ToArray(),
                Functions.ToDictionary(x => x.Name, x => x.Function.Build()));
        }
    }
}