using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Kohctpyktop.Controls;
using Kohctpyktop.Models.Field;
using Kohctpyktop.Serialization;
using Kohctpyktop.ViewModels;
using Microsoft.Win32;
using DragEventArgs = Kohctpyktop.Controls.DragEventArgs;

namespace Kohctpyktop
{
    public partial class TemplateEditWindow
    {
        public TemplateEditViewModel ViewModel { get; }
        
        private Point _prevPoint;
        private (int X, int Y) _prevPosition;
        private (int Width, int Height) _prevSize;
        private ICanvasObject _captured;
        private ResizeDirection _resizeDirection;
        
        public TemplateEditWindow(TemplateEditViewModel viewModel)
        {
            DataContext = ViewModel = viewModel;
            InitializeComponent();
        }

        private void CanvasItemMouseDown(object sender, DragEventArgs e)
        {
            var el = (FrameworkElement) sender;

            if (!(el.DataContext is ICanvasObject item))
                return;

            SelectCanvasObject(item);

            _captured = item;
            _prevPoint = e.MouseEventArgs.GetPosition(ItemsCanvas);
            _prevPosition = (item.X, item.Y);
            
            Mouse.Capture(e.OriginalSource);
        }

        private void CanvasItemMouseUp(object sender, DragEventArgs e)
        {
            if (!ReferenceEquals(Mouse.Captured, e.OriginalSource)) return;
            
            Mouse.Capture(null);
           
            MoveItem(e.MouseEventArgs.GetPosition(ItemsCanvas));
        }

        private void CanvasItemMouseMove(object sender, DragEventArgs e)
        {
            if (Mouse.Captured == null) return;
            if (!ReferenceEquals(Mouse.Captured, e.OriginalSource)) return;
            
            MoveItem(e.MouseEventArgs.GetPosition(ItemsCanvas));
        }

        private void MoveItem(Point currentPos)
        {
            var diff = currentPos - _prevPoint;
            
            var diffX = (int) Math.Round(diff.X / 13);
            var diffY = (int) Math.Round(diff.Y / 13);
            
            ViewModel.Move(_captured, _prevPosition.X + diffX, _prevPosition.Y + diffY);
        }

        private void CanvasItemResizeMouseDown(object sender, ResizeEventArgs e)
        {
            var el = (FrameworkElement) sender;
            if (!(el.DataContext is ICanvasObject item))
                return;

            SelectCanvasObject(item);

            _captured = item;
            _prevPoint = e.MouseEventArgs.GetPosition(ItemsCanvas);
            _prevPosition = (item.X, item.Y);
            _prevSize = (item.Width, item.Height);

            _resizeDirection = e.Direction;
            
            Mouse.Capture(e.OriginalSource);
        }

        private void CanvasItemResizeMouseUp(object sender, ResizeEventArgs e)
        {
            if (!ReferenceEquals(Mouse.Captured, e.OriginalSource)) return;
            
            Mouse.Capture(null);
           
            ResizeItem(e.MouseEventArgs.GetPosition(ItemsCanvas));
        }

        private void CanvasItemResizeMouseMove(object sender, ResizeEventArgs e)
        {
            if (Mouse.Captured == null) return;
            if (!ReferenceEquals(Mouse.Captured, e.OriginalSource)) return;
            
            ResizeItem(e.MouseEventArgs.GetPosition(ItemsCanvas));
        }
        
        private void ResizeItem(Point currentPos)
        {
            var diff = currentPos - _prevPoint;
            
            var diffX = (int) Math.Round(diff.X / 13);
            var diffY = (int) Math.Round(diff.Y / 13);

            var (x, y) = _prevPosition;
            var (w, h) = _prevSize;
            
            switch (_resizeDirection)
            {
                case ResizeDirection.N:
                case ResizeDirection.NW:
                case ResizeDirection.NE:
                    y += diffY;
                    h -= diffY;
                    break;
                
                case ResizeDirection.S:
                case ResizeDirection.SW:
                case ResizeDirection.SE:
                    h += diffY;
                    break;
            }
            
            switch (_resizeDirection)
            {
                case ResizeDirection.W:
                case ResizeDirection.NW:
                case ResizeDirection.SW:
                    x += diffX;
                    w -= diffX;
                    break;
                
                case ResizeDirection.E:
                case ResizeDirection.NE:
                case ResizeDirection.SE:
                    w += diffX;
                    break;
            }
            
            ViewModel.Resize(_captured, x, y, w, h);
        }

        private void TreeViewItemSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ViewModel.SelectedObject = e.NewValue;
        }

        private void SelectCanvasObject(ICanvasObject obj)
        {
            ViewModel.SelectedObject = obj;

            var item = SearchTreeItemByObject(obj, ItemsTreeView);
            if (item != null)
            {
                item.IsSelected = true;
            }
        }

        private TreeViewItem SearchTreeItemByObject(object obj, ItemsControl tree)
        {
            var item = (TreeViewItem) tree.ItemContainerGenerator.ContainerFromItem(obj);
            if (item != null) return item;

            var count = tree.ItemContainerGenerator.Items.Count;
            for (var i = 0; i < count; i++)
            {
                var subtree = (TreeViewItem) tree.ItemContainerGenerator.ContainerFromIndex(i);
                var itemInSubtree = SearchTreeItemByObject(obj, subtree);
                if (itemInSubtree != null) return itemInSubtree;
            }

            return null;
        }

        private void AddPin(object sender, RoutedEventArgs e)
        {
            ViewModel.AddPin();
        }

        private void AddDeadZone(object sender, RoutedEventArgs e)
        {
            ViewModel.AddDeadZone();
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                ViewModel.RemoveSelectedObject();
        }

        private void AddAggregatePart(object sender, RoutedEventArgs e)
        {
            var agg = (ValuesFunctionTemplate) ((FrameworkElement) sender).DataContext;
            agg.AggregateParts.Add(new ValuesFunctionTemplate());
        }

        private void RemoveAggregatePart(object sender, RoutedEventArgs e)
        {
            var agg = (ValuesFunctionTemplate) ((FrameworkElement) sender).DataContext;
            var parts = agg.AggregateParts;
            var cv = CollectionViewSource.GetDefaultView(parts);
            var sel = (ValuesFunctionTemplate) cv.CurrentItem;
            if (sel != null)
                agg.AggregateParts.Remove(sel);
        }

        private void OpenTemplate(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() ?? false)
            {
                try
                {
                    using (var file = File.OpenRead(ofd.FileName))
                    {
                        ViewModel.OpenTemplate(LayerSerializer.ReadTemplate(file));
                    }
                }
                catch (Exception ex)
                {
                    // todo: handling
                    MessageBox.Show("Failed to open layer data", "Error", MessageBoxButton.OK);
                }
            }
        }

        private void SaveTemplate(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            if (sfd.ShowDialog() ?? false)
            {
                try
                {
                    using (var file = File.OpenWrite(sfd.FileName))
                    {
                        file.SetLength(0);
                        LayerSerializer.WriteTemplate(file, ViewModel.SaveTemplate());
                    }
                }
                catch (Exception ex)
                {
                    // todo: handling
                    MessageBox.Show("Failed to save layer data", "Error", MessageBoxButton.OK);
                }
            }
        }
    }

    public class TemplateCanvasTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var el = (FrameworkElement) container;
            
            switch (item)
            {
                case PinTemplate _:
                    return (DataTemplate) el.FindResource("PinTemplate");
                case DeadZoneTemplate _:
                    return (DataTemplate) el.FindResource("DeadZoneTemplate");
                default:
                    throw new Exception("Invalid object type");
            }
        }
    }
    
    public class TemplateCanvasPropertyGridTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var el = (FrameworkElement) container;

            switch (item)
            {
                case PinTemplate _:
                    return (DataTemplate) el.FindResource("PinTemplate");
                case DeadZoneTemplate _:
                    return (DataTemplate) el.FindResource("DeadZoneTemplate");
                case TreeViewItem tvi when "INFO".Equals(tvi.Tag):
                    return (DataTemplate) el.FindResource("InfoTemplate");
                default:
                    return (DataTemplate) el.FindResource("EmptyTemplate");
            }
        }
    }

    public class TemplateCanvasStyleSelector : StyleSelector
    {
        public override Style SelectStyle(object item, DependencyObject container)
        {
            var el = (FrameworkElement) container;

            switch (item)
            {
                case PinTemplate _:
                    return (Style) el.FindResource("PinContainerStyle");
                case DeadZoneTemplate _:
                    return (Style) el.FindResource("DeadZoneContainerStyle");
                default:
                    throw new Exception("Invalid object type");
            }
        }
    }
}
