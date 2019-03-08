using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using Kohctpyktop.ViewModels;

namespace Kohctpyktop.Controls
{
    public enum ResizeDirection { NW, SE, NE, SW, W, E, N, S }

    public class DragEventArgs
    {
        public DragEventArgs(MouseEventArgs mouseEventArgs, IInputElement originalSource)
        {
            MouseEventArgs = mouseEventArgs;
            OriginalSource = originalSource;
        }

        public MouseEventArgs MouseEventArgs { get; }
        public IInputElement OriginalSource { get; }
    }
    
    public class ResizeEventArgs
    {
        public ResizeEventArgs(MouseEventArgs mouseEventArgs, ResizeDirection direction, IInputElement originalSource)
        {
            MouseEventArgs = mouseEventArgs;
            Direction = direction;
            OriginalSource = originalSource;
        }

        public MouseEventArgs MouseEventArgs { get; }
        public ResizeDirection Direction { get; }
        public IInputElement OriginalSource { get; }
    }
    
    [ContentProperty(nameof(Child))]
    public partial class CanvasObjectControl
    {
        public CanvasObjectControl()
        {
            InitializeComponent();
        }

        public event EventHandler<DragEventArgs> DragDown;
        public event EventHandler<DragEventArgs> DragUp;
        public event EventHandler<DragEventArgs> DragMove;
        
        public event EventHandler<ResizeEventArgs> ResizeDown;
        public event EventHandler<ResizeEventArgs> ResizeUp;
        public event EventHandler<ResizeEventArgs> ResizeMove;

        public static readonly DependencyProperty SelectedCanvasObjectProperty = DependencyProperty.Register(
            "SelectedCanvasObject", typeof(ICanvasObject), typeof(CanvasObjectControl), new PropertyMetadata(default(ICanvasObject)));

        public ICanvasObject SelectedCanvasObject
        {
            get => (ICanvasObject) GetValue(SelectedCanvasObjectProperty);
            set => SetValue(SelectedCanvasObjectProperty, value);
        }

        public static readonly DependencyProperty CanvasObjectProperty = DependencyProperty.Register(
            "CanvasObject", typeof(ICanvasObject), typeof(CanvasObjectControl), new PropertyMetadata(default(ICanvasObject)));

        public ICanvasObject CanvasObject
        {
            get => (ICanvasObject) GetValue(CanvasObjectProperty);
            set => SetValue(CanvasObjectProperty, value);
        }

        public static readonly DependencyProperty ChildProperty = DependencyProperty.Register(
            "Child", typeof(FrameworkElement), typeof(CanvasObjectControl), new PropertyMetadata(default(FrameworkElement)));

        public FrameworkElement Child
        {
            get { return (FrameworkElement) GetValue(ChildProperty); }
            set { SetValue(ChildProperty, value); }
        }
        
        private void ResizeMouseDown(object sender, MouseButtonEventArgs e)
        {
            var dir = (ResizeDirection) Enum.Parse(typeof(ResizeDirection), (string) ((FrameworkElement) sender).Tag);
            ResizeDown?.Invoke(this, new ResizeEventArgs(e, dir, sender as IInputElement));
        }

        private void ResizeMouseUp(object sender, MouseButtonEventArgs e)
        {
            var dir = (ResizeDirection) Enum.Parse(typeof(ResizeDirection), (string) ((FrameworkElement) sender).Tag);
            ResizeUp?.Invoke(this, new ResizeEventArgs(e, dir, sender as IInputElement));
        }

        private void ResizeMouseMove(object sender, MouseEventArgs e)
        {
            var dir = (ResizeDirection) Enum.Parse(typeof(ResizeDirection), (string) ((FrameworkElement) sender).Tag);
            ResizeMove?.Invoke(this, new ResizeEventArgs(e, dir, sender as IInputElement));
        }

        private new void MouseDown(object sender, MouseButtonEventArgs e) => DragDown?.Invoke(this, new DragEventArgs(e, sender as IInputElement));
        private new void MouseUp(object sender, MouseButtonEventArgs e) => DragUp?.Invoke(this, new DragEventArgs(e, sender as IInputElement));
        private new void MouseMove(object sender, MouseEventArgs e) => DragMove?.Invoke(this, new DragEventArgs(e, sender as IInputElement));
    }
}
